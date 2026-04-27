#!/usr/bin/env python3
"""Tune unit template stats for one secondary role using eval telemetry."""
from __future__ import annotations

import argparse
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.measurement_models as measurements
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
import auto_balancer.workflows.role_stats as role_stats
from auto_balancer.config import load_balancer_config_from_args
from balancing_scripts.secondary_roles.scoring import compute_primary_role_value_score, compute_secondary_role_score


# ── Bounds ────────────────────────────────────────────────────────────────────

StatBounds = role_stats.StatBounds


def _pct_bounds_stat(initial: int, max_pct: float, floor: int) -> tuple[int, int]:
    """Return [initial × (1 − max_pct), initial × (1 + max_pct)] floored at `floor`."""
    return role_stats.pct_bounds_stat(initial, max_pct, floor)


def compute_stat_bounds(
    config: config_models.SecondaryRoleBalancerConfig,
    initial: role_stats.StatCandidate,
) -> role_stats.StatBounds:
    """Derive per-stat search windows from the authored initial values and config pct."""
    hp_init, mana_init, move_init, phys_dr_init, magic_dr_init = initial
    b = config.balance
    return role_stats.StatBounds(
        hp=_pct_bounds_stat(hp_init, b.hp_max_pct_change, b.hp_floor),
        mana=_pct_bounds_stat(mana_init, b.mana_max_pct_change, b.mana_floor),
        move=_pct_bounds_stat(move_init, b.move_max_pct_change, b.move_floor),
        phys_dr=_pct_bounds_stat(phys_dr_init, b.dr_max_pct_change, b.dr_floor),
        magic_dr=_pct_bounds_stat(magic_dr_init, b.dr_max_pct_change, b.dr_floor),
    )


# ── Evaluator ─────────────────────────────────────────────────────────────────

class SecondaryRoleCandidateEvaluator:
    def __init__(
        self,
        config: config_models.SecondaryRoleBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
        bounds: role_stats.StatBounds,
    ):
        self._config = config
        self._content_path = content_path
        self._eval_config = eval_config
        self._offensive_ability_ids = offensive_ability_ids
        self._bounds = bounds
        self._cache: dict[role_stats.StatCandidate, measurements.SecondaryRoleMeasurement] = {}

    def evaluate(self, candidate: role_stats.StatCandidate) -> measurements.SecondaryRoleMeasurement:
        normalized_candidate = normalize_candidate(self._bounds, candidate)
        if normalized_candidate not in self._cache:
            self._cache[normalized_candidate] = evaluate_candidate(
                self._config,
                self._content_path,
                self._eval_config,
                self._offensive_ability_ids,
                self._bounds,
                normalized_candidate,
            )
        return self._cache[normalized_candidate]


# ── Config loading ────────────────────────────────────────────────────────────

def load_balancer_config(args: argparse.Namespace) -> config_models.SecondaryRoleBalancerConfig:
    return load_balancer_config_from_args(
        config_models.SecondaryRoleBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


# ── Validation ────────────────────────────────────────────────────────────────

def validate_config(config: config_models.SecondaryRoleBalancerConfig) -> None:
    if not config.balance.target_secondary_role:
        raise ValueError("Secondary role balancer target_secondary_role must be set.")
    if config.scenario.map_width_tiles <= 0 or config.scenario.map_height_tiles <= 0:
        raise ValueError("Secondary role balancer map dimensions must be positive.")
    if config.ga.evaluation_turn_budget <= 0:
        raise ValueError("Secondary role balancer evaluation_turn_budget must be positive.")
    if config.ga.candidate_population_size <= 0 or config.ga.generation_count < 0:
        raise ValueError("Secondary role balancer population config is invalid.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Secondary role balancer generated_scenario_count must be positive.")
    if config.ga.evaluation_log_mode not in {"quiet", "normal", "verbose"}:
        raise ValueError("Secondary role balancer evaluation_log_mode must be 'quiet', 'normal', or 'verbose'.")
    validate_target_bounds(config)
    _validate_pct("hp_max_pct_change", config.balance.hp_max_pct_change)
    _validate_pct("mana_max_pct_change", config.balance.mana_max_pct_change)
    _validate_pct("move_max_pct_change", config.balance.move_max_pct_change)
    _validate_pct("dr_max_pct_change", config.balance.dr_max_pct_change)
    weight_sum = (
        config.balance.turn_limit_rate_fitness_weight
        + config.balance.average_attacker_turn_count_fitness_weight
        + config.balance.average_action_count_fitness_weight
        + config.balance.primary_role_value_fitness_weight
        + config.balance.secondary_role_alignment_fitness_weight
    )
    if abs(weight_sum - 1.0) > 1e-6:
        raise ValueError(f"Secondary role balancer fitness weights must sum to 1.0, got {weight_sum:.6f}.")


def _validate_pct(name: str, val: float) -> None:
    if not (0.0 < val <= 1.0):
        raise ValueError(f"Secondary role balancer {name} must be in (0, 1], got {val}.")


def validate_target_bounds(config: config_models.SecondaryRoleBalancerConfig) -> None:
    target_bounds = [
        (config.balance.turn_limit_rate_target_min, config.balance.turn_limit_rate_target_max, "turn-limit"),
        (config.balance.average_attacker_turn_count_target_min, config.balance.average_attacker_turn_count_target_max, "average-attacker-turn-count"),
        (config.balance.average_action_count_target_min, config.balance.average_action_count_target_max, "action-count"),
    ]
    for target_low, target_high, target_name in target_bounds:
        if target_low > target_high:
            raise ValueError(f"Secondary role balancer {target_name} target bounds are invalid.")


# ── Content helpers ───────────────────────────────────────────────────────────

def prepare_eval_content(
    config: config_models.SecondaryRoleBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    return role_stats.prepare_eval_content_from_config(config, source_content_path)


def build_eval_config(config: config_models.SecondaryRoleBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
    return role_stats.build_role_alignment_eval_config(config, content_path)


def load_initial_candidate(config: config_models.SecondaryRoleBalancerConfig, content_path: Path) -> role_stats.StatCandidate:
    return role_stats.load_initial_stat_candidate(
        content_path,
        config.balance.target_primary_role,
        config.balance.target_secondary_role,
    )


# ── Candidate helpers ─────────────────────────────────────────────────────────

def normalize_candidate(
    bounds: role_stats.StatBounds,
    candidate: role_stats.StatCandidate,
) -> role_stats.StatCandidate:
    return role_stats.normalize_candidate(bounds, candidate)


def build_field_values(candidate: role_stats.StatCandidate) -> dict[str, int]:
    return role_stats.build_field_values(candidate)


# ── Evaluation ────────────────────────────────────────────────────────────────

def evaluate_candidate(
    config: config_models.SecondaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    bounds: role_stats.StatBounds,
    candidate: role_stats.StatCandidate,
    repeat_stages: tuple[eval_api.RepeatStage, ...] | None = None,
) -> measurements.SecondaryRoleMeasurement:
    normalized_candidate = normalize_candidate(bounds, candidate)
    apply_candidate_to_content(config, content_path, normalized_candidate)
    staged_repeats = config.ga.evaluation_repeat_stages if repeat_stages is None else repeat_stages

    try:
        summary = run_eval_role_alignment_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            staged_repeats,
            offensive_ability_ids,
        )
    except Exception as exc:  # pragma: no cover
        return measurements.SecondaryRoleMeasurement(
            *normalized_candidate,
            attacker_win_rate=0.0,
            turn_limit_rate=1.0,
            average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
            average_action_count=float(config.ga.evaluation_turn_budget * 6),
            primary_role_value_score=-10.0,
            secondary_role_alignment_score=-10.0,
            raw_fitness=-10.0,
            fitness=-10.0,
            error_message=str(exc),
        )

    return build_measurement(config, content_path, summary, normalized_candidate)


def run_eval_role_alignment_with_stages(
    eval_config: eval_api.EvalCommandConfig,
    turn_budget: int,
    repeat_stages: tuple[eval_api.RepeatStage, ...],
    offensive_ability_ids: set[str],
) -> eval_api.EvalRoleAlignmentSummary:
    return role_stats.run_eval_role_alignment_with_stages(
        eval_config,
        turn_budget,
        repeat_stages,
        offensive_ability_ids,
    )


def apply_candidate_to_content(
    config: config_models.SecondaryRoleBalancerConfig,
    content_path: Path,
    candidate: role_stats.StatCandidate,
) -> None:
    role_stats.apply_candidate_to_content(
        content_path,
        config.balance.target_primary_role,
        config.balance.target_secondary_role,
        candidate,
    )


def build_measurement(
    config: config_models.SecondaryRoleBalancerConfig,
    content_path: Path,
    summary: eval_api.EvalRoleAlignmentSummary,
    candidate: role_stats.StatCandidate,
) -> measurements.SecondaryRoleMeasurement:
    unit_max_hp, _, unit_move_points, _, _ = candidate
    detailed = summary.detailed
    attacker_win_rate = detailed.attacker_wins / detailed.total_runs
    turn_limit_rate = detailed.turn_limit_count / detailed.total_runs
    secondary_role_alignment_score = compute_secondary_role_score(
        config.balance.target_secondary_role,
        config.balance.target_primary_role,
        summary,
        unit_max_hp,
        unit_move_points,
        scenarios.load_unit_templates(content_path),
    )

    primary_role_value_score = compute_primary_role_value_score(config.balance, summary)

    turn_limit_fitness = ga.compute_target_band_fitness(
        turn_limit_rate,
        config.balance.turn_limit_rate_target_min,
        config.balance.turn_limit_rate_target_max,
    )
    attacker_turns_fitness = ga.compute_target_band_fitness(
        detailed.average_attacker_turn_count,
        config.balance.average_attacker_turn_count_target_min,
        config.balance.average_attacker_turn_count_target_max,
    )
    action_count_fitness = ga.compute_target_band_fitness(
        detailed.average_action_count,
        config.balance.average_action_count_target_min,
        config.balance.average_action_count_target_max,
    )

    raw_fitness = (
        turn_limit_fitness * config.balance.turn_limit_rate_fitness_weight
        + attacker_turns_fitness * config.balance.average_attacker_turn_count_fitness_weight
        + action_count_fitness * config.balance.average_action_count_fitness_weight
        + primary_role_value_score * config.balance.primary_role_value_fitness_weight
        + secondary_role_alignment_score * config.balance.secondary_role_alignment_fitness_weight
    )

    return measurements.SecondaryRoleMeasurement(
        *candidate,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=detailed.average_attacker_turn_count,
        average_action_count=detailed.average_action_count,
        primary_role_value_score=primary_role_value_score,
        secondary_role_alignment_score=secondary_role_alignment_score,
        raw_fitness=raw_fitness,
        fitness=raw_fitness,
        error_message=None,
    )


# ── GA ────────────────────────────────────────────────────────────────────────

def run_secondary_role_ga(
    config: config_models.SecondaryRoleBalancerConfig,
    bounds: role_stats.StatBounds,
    candidate_evaluator,
    initial_candidate: role_stats.StatCandidate,
) -> measurements.SecondaryRoleMeasurement:
    seed_candidates = [
        (initial_candidate[0], bounds.mana[1], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
        (initial_candidate[0], bounds.mana[0], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
    ]
    return role_stats.run_stat_ga(
        creator_name_prefix="SecondaryRole",
        random_seed=config.ga.ga_random_seed,
        population_size=config.ga.candidate_population_size,
        generation_count=config.ga.generation_count,
        mutation_probability=config.ga.mutation_probability,
        bounds=bounds,
        initial_candidate=initial_candidate,
        seed_candidates=seed_candidates,
        candidate_evaluator=candidate_evaluator,
        get_fitness=lambda measurement: measurement.fitness,
        on_candidate=lambda measurement: print_candidate_measurement(config, measurement),
        on_generation_best=print_generation_best,
    )


# ── Public API ────────────────────────────────────────────────────────────────

def optimize_secondary_role(
    config: config_models.SecondaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.SecondaryRoleMeasurement:
    validate_config(config)
    initial_candidate = load_initial_candidate(config, content_path)
    bounds = compute_stat_bounds(config, initial_candidate)

    print(
        f"secondary-role GA: {config.balance.target_primary_role or '*'}/{config.balance.target_secondary_role} "
        f"initial=({initial_candidate[0]},{initial_candidate[1]},{initial_candidate[2]},{initial_candidate[3]},{initial_candidate[4]}) "
        f"hp=[{bounds.hp[0]},{bounds.hp[1]}] mana=[{bounds.mana[0]},{bounds.mana[1]}] "
        f"move=[{bounds.move[0]},{bounds.move[1]}] "
        f"physDR=[{bounds.phys_dr[0]},{bounds.phys_dr[1]}] magicDR=[{bounds.magic_dr[0]},{bounds.magic_dr[1]}]",
        flush=True,
    )

    candidate_evaluator = SecondaryRoleCandidateEvaluator(config, content_path, eval_config, offensive_ability_ids, bounds)
    best_measurement = run_secondary_role_ga(config, bounds, candidate_evaluator.evaluate, initial_candidate)
    apply_candidate_to_content(
        config,
        content_path,
        (
            best_measurement.unit_max_hp,
            best_measurement.unit_max_mana_points,
            best_measurement.unit_move_points,
            best_measurement.physical_damage_received_percent,
            best_measurement.magic_damage_received_percent,
        ),
    )
    return best_measurement


# ── Logging helpers ───────────────────────────────────────────────────────────

def print_candidate_measurement(
    config: config_models.SecondaryRoleBalancerConfig,
    measurement: measurements.SecondaryRoleMeasurement,
) -> None:
    primary_filter = config.balance.target_primary_role or "*"
    reporting.print_record(
        "candidate",
        [
            reporting.field("secondary-role", config.balance.target_secondary_role),
            reporting.field("primary-role", primary_filter),
            *reporting.secondary_role_fields(measurement),
        ],
    )


def print_generation_best(generation: int, measurement: measurements.SecondaryRoleMeasurement) -> None:
    reporting.print_record(f"generation {generation} best", reporting.secondary_role_fields(measurement))


def run(config: config_models.SecondaryRoleBalancerConfig) -> int:
    runtime.ensure_deap_available()

    validate_config(config)
    content_path = prepare_eval_content(config)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    best_measurement = optimize_secondary_role(config, content_path, eval_config, offensive_ability_ids)

    primary_filter = config.balance.target_primary_role or "*"
    reporting.print_record(
        "best",
        [
            reporting.field("secondary-role", config.balance.target_secondary_role),
            reporting.field("primary-role", primary_filter),
            *reporting.secondary_role_fields(best_measurement),
        ],
    )
    print(f"turn-count={config.ga.evaluation_turn_budget}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0
