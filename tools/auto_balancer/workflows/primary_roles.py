#!/usr/bin/env python3
"""Tune unit template stats for one primary role using eval telemetry."""
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
from balancing_scripts.primary_roles.scoring import compute_primary_role_score


class PrimaryRoleCandidateEvaluator:
    def __init__(
        self,
        config: config_models.PrimaryRoleBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
    ):
        self._config = config
        self._content_path = content_path
        self._eval_config = eval_config
        self._offensive_ability_ids = offensive_ability_ids
        self._bounds = build_stat_bounds(config)
        self._cache: dict[role_stats.StatCandidate, measurements.PrimaryRoleMeasurement] = {}

    def evaluate(self, candidate: role_stats.StatCandidate) -> measurements.PrimaryRoleMeasurement:
        normalized_candidate = normalize_candidate(self._config, candidate)
        if normalized_candidate not in self._cache:
            self._cache[normalized_candidate] = evaluate_candidate(
                self._config,
                self._content_path,
                self._eval_config,
                self._offensive_ability_ids,
                normalized_candidate,
        )
        return self._cache[normalized_candidate]


def load_balancer_config(args: argparse.Namespace) -> config_models.PrimaryRoleBalancerConfig:
    return load_balancer_config_from_args(
        config_models.PrimaryRoleBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def validate_config(config: config_models.PrimaryRoleBalancerConfig) -> None:
    if not config.balance.target_primary_role:
        raise ValueError("Primary role balancer target_primary_role must be set.")
    if config.scenario.map_width_tiles <= 0 or config.scenario.map_height_tiles <= 0:
        raise ValueError("Primary role balancer map dimensions must be positive.")
    if config.ga.evaluation_turn_budget <= 0:
        raise ValueError("Primary role balancer evaluation_turn_budget must be positive.")
    if config.ga.candidate_population_size <= 0 or config.ga.generation_count < 0:
        raise ValueError("Primary role balancer population config is invalid.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Primary role balancer generated_scenario_count must be positive.")
    if config.ga.evaluation_log_mode not in {"quiet", "normal", "verbose"}:
        raise ValueError("Primary role balancer evaluation_log_mode must be 'quiet', 'normal', or 'verbose'.")
    validate_target_bounds(config)


def validate_target_bounds(config: config_models.PrimaryRoleBalancerConfig) -> None:
    target_bounds = [
        (config.balance.turn_limit_rate_target_min, config.balance.turn_limit_rate_target_max, "turn-limit"),
        (config.balance.average_attacker_turn_count_target_min, config.balance.average_attacker_turn_count_target_max, "average-attacker-turn-count"),
        (config.balance.average_action_count_target_min, config.balance.average_action_count_target_max, "action-count"),
    ]
    for target_low, target_high, target_name in target_bounds:
        if target_low > target_high:
            raise ValueError(f"Primary role balancer {target_name} target bounds are invalid.")


def prepare_eval_content(
    config: config_models.PrimaryRoleBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    return role_stats.prepare_eval_content_from_config(config, source_content_path)


def build_eval_config(config: config_models.PrimaryRoleBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
    return role_stats.build_role_alignment_eval_config(config, content_path)


def load_initial_candidate(config: config_models.PrimaryRoleBalancerConfig, content_path: Path) -> role_stats.StatCandidate:
    return role_stats.load_initial_stat_candidate(content_path, config.balance.target_primary_role, None)


def build_stat_bounds(config: config_models.PrimaryRoleBalancerConfig) -> role_stats.StatBounds:
    return role_stats.StatBounds(
        hp=(config.balance.unit_max_hp_min, config.balance.unit_max_hp_max),
        mana=(config.balance.unit_max_mana_points_min, config.balance.unit_max_mana_points_max),
        move=(config.balance.unit_move_points_min, config.balance.unit_move_points_max),
        phys_dr=(
            config.balance.physical_damage_received_percent_min,
            config.balance.physical_damage_received_percent_max,
        ),
        magic_dr=(
            config.balance.magic_damage_received_percent_min,
            config.balance.magic_damage_received_percent_max,
        ),
    )


def normalize_candidate(
    config: config_models.PrimaryRoleBalancerConfig,
    candidate: role_stats.StatCandidate,
) -> role_stats.StatCandidate:
    return role_stats.normalize_candidate(build_stat_bounds(config), candidate)


def build_field_values(candidate: role_stats.StatCandidate) -> dict[str, int]:
    return role_stats.build_field_values(candidate)


def evaluate_candidate(
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    candidate: role_stats.StatCandidate,
    repeat_stages: tuple[eval_api.RepeatStage, ...] | None = None,
) -> measurements.PrimaryRoleMeasurement:
    normalized_candidate = normalize_candidate(config, candidate)
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
        return measurements.PrimaryRoleMeasurement(
            *normalized_candidate,
            attacker_win_rate=0.0,
            turn_limit_rate=1.0,
            average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
            average_action_count=float(config.ga.evaluation_turn_budget * 6),
            primary_role_alignment_score=-10.0,
            raw_fitness=-10.0,
            fitness=-10.0,
            error_message=str(exc),
        )

    return build_measurement(config, summary, normalized_candidate)


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
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    candidate: role_stats.StatCandidate,
) -> None:
    role_stats.apply_candidate_to_content(
        content_path,
        config.balance.target_primary_role,
        None,
        normalize_candidate(config, candidate),
    )


def build_measurement(
    config: config_models.PrimaryRoleBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
    candidate: role_stats.StatCandidate,
) -> measurements.PrimaryRoleMeasurement:
    detailed = summary.detailed
    attacker_win_rate = detailed.attacker_wins / detailed.total_runs
    turn_limit_rate = detailed.turn_limit_count / detailed.total_runs
    primary_role_alignment_score = compute_primary_role_score(config.balance.target_primary_role, summary)

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
        + primary_role_alignment_score * config.balance.primary_role_alignment_fitness_weight
    )

    return measurements.PrimaryRoleMeasurement(
        *candidate,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=detailed.average_attacker_turn_count,
        average_action_count=detailed.average_action_count,
        primary_role_alignment_score=primary_role_alignment_score,
        raw_fitness=raw_fitness,
        fitness=raw_fitness,
        error_message=None,
    )

def run_primary_role_ga(
    config: config_models.PrimaryRoleBalancerConfig,
    candidate_evaluator,
    initial_candidate: role_stats.StatCandidate,
) -> measurements.PrimaryRoleMeasurement:
    seed_candidates = [
        (config.balance.unit_max_hp_max, initial_candidate[1], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
        (config.balance.unit_max_hp_min, initial_candidate[1], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
    ]
    return role_stats.run_stat_ga(
        creator_name_prefix="PrimaryRole",
        random_seed=config.ga.ga_random_seed,
        population_size=config.ga.candidate_population_size,
        generation_count=config.ga.generation_count,
        mutation_probability=config.ga.mutation_probability,
        bounds=build_stat_bounds(config),
        initial_candidate=initial_candidate,
        seed_candidates=seed_candidates,
        mutation_step_sizes=[18, 8, 1, 10, 10],
        candidate_evaluator=candidate_evaluator,
        get_fitness=lambda measurement: measurement.fitness,
        on_candidate=lambda measurement: print_candidate_measurement(config, measurement),
        on_generation_best=print_generation_best,
    )


def print_candidate_measurement(config: config_models.PrimaryRoleBalancerConfig, measurement: measurements.PrimaryRoleMeasurement) -> None:
    reporting.print_record(
        "candidate",
        [
            reporting.field("role", config.balance.target_primary_role),
            *reporting.primary_role_fields(measurement),
        ],
    )


def print_generation_best(generation: int, measurement: measurements.PrimaryRoleMeasurement) -> None:
    reporting.print_record(f"generation {generation} best", reporting.primary_role_fields(measurement))


def optimize_primary_role(
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.PrimaryRoleMeasurement:
    validate_config(config)
    initial_candidate = load_initial_candidate(config, content_path)
    candidate_evaluator = PrimaryRoleCandidateEvaluator(config, content_path, eval_config, offensive_ability_ids)

    best_measurement = run_primary_role_ga(config, candidate_evaluator.evaluate, initial_candidate)
    # Candidate evaluation edits the working content pack each time, so leave it on the selected result.
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


def run(config: config_models.PrimaryRoleBalancerConfig) -> int:
    runtime.ensure_deap_available()

    # Work on generated content so repeated GA runs do not mutate the checked-in seed content.
    validate_config(config)
    content_path = prepare_eval_content(config)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    best_measurement = optimize_primary_role(config, content_path, eval_config, offensive_ability_ids)

    reporting.print_record(
        "best",
        [
            reporting.field("role", config.balance.target_primary_role),
            *reporting.primary_role_fields(best_measurement),
        ],
    )
    print(f"turn-count={config.ga.evaluation_turn_budget}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0
