#!/usr/bin/env python3
"""Tune Tank, Damage, and Healer baselines as one combined candidate."""
from __future__ import annotations

import random
from dataclasses import dataclass
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.measurement_models as measurements
import auto_balancer.reporting as reporting
import auto_balancer.scenarios as scenarios
import auto_balancer.workflows.primary_roles as primary_roles
import auto_balancer.workflows.role_stats as role_stats
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow
from balancing_scripts.primary_roles.scoring import compute_primary_role_score


ROLE_ORDER: tuple[tuple[str, str], ...] = (
    ("Tank", "tank"),
    ("Damage", "damage"),
    ("Healer", "healer"),
)
STAT_COUNT = 5
CombinedPrimaryCandidate = tuple[int, ...]


@dataclass(frozen=True)
class RoleSpec:
    role_name: str
    section_name: str
    config: config_models.PrimaryRoleBalancerConfig
    bounds: role_stats.StatBounds
    initial_candidate: role_stats.StatCandidate


class CombinedPrimaryRoleWorkflow(CandidateWorkflow[CombinedPrimaryCandidate, measurements.CombinedPrimaryRoleMeasurement]):
    def __init__(
        self,
        *,
        config: config_models.NestedPrimaryRoleBalancerConfig,
        role_specs: tuple[RoleSpec, ...],
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
    ):
        self.creator_name_prefix = "CombinedPrimaryRole"
        self.random_seed = config.ga.ga_random_seed
        self.population_size = config.ga.candidate_population_size
        self.generation_count = config.ga.generation_count
        self.mutation_probability = config.ga.mutation_probability
        self.crossover_probability = config.ga.crossover_probability
        self.config = config
        self.role_specs = role_specs
        self.content_path = content_path
        self.eval_config = eval_config
        self.offensive_ability_ids = offensive_ability_ids
        self.initial_candidate = tuple(
            value
            for spec in role_specs
            for value in spec.initial_candidate
        )

    def normalize_individual(self, individual: list[int]) -> CombinedPrimaryCandidate:
        candidate = tuple(int(value) for value in individual)
        normalized: list[int] = []
        for spec, role_candidate in zip(self.role_specs, split_candidate(candidate), strict=True):
            normalized.extend(role_stats.normalize_candidate(spec.bounds, role_candidate))
        return tuple(normalized)

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        population: list = [individual_type(list(self.initial_candidate))]
        seen: set[CombinedPrimaryCandidate] = {self.initial_candidate}

        seed_candidates = build_seed_candidates(self.role_specs)
        for candidate in seed_candidates:
            normalized = self.normalize_individual(list(candidate))
            if normalized in seen:
                continue
            seen.add(normalized)
            population.append(individual_type(list(normalized)))
            if len(population) >= self.population_size:
                return population[: self.population_size]

        while len(population) < self.population_size:
            candidate = []
            for spec in self.role_specs:
                candidate.extend(
                    [
                        rng.randint(*spec.bounds.hp),
                        rng.randint(*spec.bounds.mana),
                        rng.randint(*spec.bounds.move),
                        rng.randint(*spec.bounds.phys_dr),
                        rng.randint(*spec.bounds.magic_dr),
                    ]
                )
            normalized = self.normalize_individual(candidate)
            if normalized in seen:
                continue
            seen.add(normalized)
            population.append(individual_type(list(normalized)))

        return population[: self.population_size]

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        for role_index, spec in enumerate(self.role_specs):
            start = role_index * STAT_COUNT
            role_values = individual[start : start + STAT_COUNT]
            mutated = role_stats.mutate_candidate(role_values, spec.bounds, rng, [10, 6, 1, 3, 3])[0]
            individual[start : start + STAT_COUNT] = mutated
        return (individual,)

    def evaluate_candidate(self, candidate: CombinedPrimaryCandidate) -> measurements.CombinedPrimaryRoleMeasurement:
        return evaluate_candidate(
            self.config,
            self.role_specs,
            self.content_path,
            self.eval_config,
            self.offensive_ability_ids,
            candidate,
        )

    def get_fitness(self, measurement: measurements.CombinedPrimaryRoleMeasurement) -> float:
        return measurement.fitness

    def on_candidate(
        self,
        measurement: measurements.CombinedPrimaryRoleMeasurement,
        elapsed_seconds: float,
        cached: bool,
    ) -> None:
        print_candidate_measurement(measurement, elapsed_seconds, cached)

    def on_generation_best(self, generation: int, measurement: measurements.CombinedPrimaryRoleMeasurement) -> None:
        reporting.print_section(f"gen-best {generation}", combined_primary_role_fields(measurement))


def build_role_specs(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    build_primary_role_config,
    content_path: Path,
) -> tuple[RoleSpec, ...]:
    specs: list[RoleSpec] = []
    for role_name, section_name in ROLE_ORDER:
        role_config = build_primary_role_config(config, role_name, section_name)
        specs.append(
            RoleSpec(
                role_name=role_name,
                section_name=section_name,
                config=role_config,
                bounds=primary_roles.build_stat_bounds(role_config),
                initial_candidate=primary_roles.load_initial_candidate(role_config, content_path),
            )
        )
    return tuple(specs)


def build_eval_config(config: config_models.PrimaryRoleBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
    return primary_roles.build_eval_config(config, content_path)


def prepare_eval_content(
    config: config_models.PrimaryRoleBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    return primary_roles.prepare_eval_content(config, source_content_path)


def evaluate_current_content(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    role_specs: tuple[RoleSpec, ...],
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.CombinedPrimaryRoleMeasurement:
    candidate = tuple(
        value
        for spec in role_specs
        for value in primary_roles.load_initial_candidate(spec.config, content_path)
    )
    try:
        summary = primary_roles.run_eval_role_alignment_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            config.ga.evaluation_repeat_stages,
            offensive_ability_ids,
        )
    except Exception as exc:  # pragma: no cover
        return build_error_measurement(config, candidate, str(exc))

    return build_measurement(config, summary, candidate)


def evaluate_candidate(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    role_specs: tuple[RoleSpec, ...],
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    candidate: CombinedPrimaryCandidate,
) -> measurements.CombinedPrimaryRoleMeasurement:
    normalized = normalize_combined_candidate(role_specs, candidate)
    apply_combined_candidate_to_content(role_specs, content_path, normalized)

    try:
        summary = primary_roles.run_eval_role_alignment_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            config.ga.evaluation_repeat_stages,
            offensive_ability_ids,
        )
    except Exception as exc:  # pragma: no cover
        return build_error_measurement(config, normalized, str(exc))

    return build_measurement(config, summary, normalized)


def optimize_combined_primary_roles(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    role_specs: tuple[RoleSpec, ...],
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.CombinedPrimaryRoleMeasurement:
    workflow = CombinedPrimaryRoleWorkflow(
        config=config,
        role_specs=role_specs,
        content_path=content_path,
        eval_config=eval_config,
        offensive_ability_ids=offensive_ability_ids,
    )
    best_key, best_measurement = run_candidate_workflow(workflow)
    apply_combined_candidate_to_content(role_specs, content_path, best_key)
    return best_measurement


def normalize_combined_candidate(
    role_specs: tuple[RoleSpec, ...],
    candidate: CombinedPrimaryCandidate,
) -> CombinedPrimaryCandidate:
    normalized: list[int] = []
    for spec, role_candidate in zip(role_specs, split_candidate(candidate), strict=True):
        normalized.extend(role_stats.normalize_candidate(spec.bounds, role_candidate))
    return tuple(normalized)


def split_candidate(candidate: CombinedPrimaryCandidate) -> tuple[role_stats.StatCandidate, ...]:
    if len(candidate) != len(ROLE_ORDER) * STAT_COUNT:
        raise ValueError(f"Expected {len(ROLE_ORDER) * STAT_COUNT} combined primary role genes, got {len(candidate)}.")
    return tuple(
        tuple(candidate[index : index + STAT_COUNT])  # type: ignore[return-value]
        for index in range(0, len(candidate), STAT_COUNT)
    )


def apply_combined_candidate_to_content(
    role_specs: tuple[RoleSpec, ...],
    content_path: Path,
    candidate: CombinedPrimaryCandidate,
) -> None:
    for spec, role_candidate in zip(role_specs, split_candidate(candidate), strict=True):
        role_stats.apply_candidate_to_content(content_path, spec.role_name, None, role_candidate)


def build_seed_candidates(role_specs: tuple[RoleSpec, ...]) -> list[CombinedPrimaryCandidate]:
    candidates: list[CombinedPrimaryCandidate] = []
    for stat_index in (0,):
        for use_max in (True, False):
            candidate = []
            for spec in role_specs:
                values = list(spec.initial_candidate)
                bounds = (spec.bounds.hp, spec.bounds.mana, spec.bounds.move, spec.bounds.phys_dr, spec.bounds.magic_dr)
                values[stat_index] = bounds[stat_index][1 if use_max else 0]
                candidate.extend(values)
            candidates.append(tuple(candidate))
    return candidates


def build_measurement(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
    candidate: CombinedPrimaryCandidate,
) -> measurements.CombinedPrimaryRoleMeasurement:
    detailed = summary.detailed
    role_scores = {
        role_name: compute_primary_role_score(role_name, summary)
        for role_name, _ in ROLE_ORDER
    }
    average_role_score = sum(role_scores.values()) / len(role_scores)

    balance = config.balance.tank
    turn_limit_rate = detailed.turn_limit_count / detailed.total_runs
    turn_limit_fitness = ga.compute_target_band_fitness(
        turn_limit_rate,
        balance.turn_limit_rate_target_min,
        balance.turn_limit_rate_target_max,
    )
    attacker_turns_fitness = ga.compute_target_band_fitness(
        detailed.average_attacker_turn_count,
        balance.average_attacker_turn_count_target_min,
        balance.average_attacker_turn_count_target_max,
    )
    action_count_fitness = ga.compute_target_band_fitness(
        detailed.average_action_count,
        balance.average_action_count_target_min,
        balance.average_action_count_target_max,
    )
    raw_fitness = (
        turn_limit_fitness * balance.turn_limit_rate_fitness_weight
        + attacker_turns_fitness * balance.average_attacker_turn_count_fitness_weight
        + action_count_fitness * balance.average_action_count_fitness_weight
        + average_role_score * balance.primary_role_alignment_fitness_weight
    )

    return measurements.CombinedPrimaryRoleMeasurement(
        *candidate,
        attacker_win_rate=detailed.attacker_wins / detailed.total_runs,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=detailed.average_attacker_turn_count,
        average_action_count=detailed.average_action_count,
        tank_primary_role_score=role_scores["Tank"],
        damage_primary_role_score=role_scores["Damage"],
        healer_primary_role_score=role_scores["Healer"],
        average_primary_role_score=average_role_score,
        raw_fitness=raw_fitness,
        fitness=raw_fitness,
        error_message=None,
    )


def build_error_measurement(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    candidate: CombinedPrimaryCandidate,
    error_message: str,
) -> measurements.CombinedPrimaryRoleMeasurement:
    return measurements.CombinedPrimaryRoleMeasurement(
        *candidate,
        attacker_win_rate=0.0,
        turn_limit_rate=1.0,
        average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
        average_action_count=float(config.ga.evaluation_turn_budget * 6),
        tank_primary_role_score=-10.0,
        damage_primary_role_score=-10.0,
        healer_primary_role_score=-10.0,
        average_primary_role_score=-10.0,
        raw_fitness=-10.0,
        fitness=-10.0,
        error_message=error_message,
    )


def combined_primary_role_fields(measurement: measurements.CombinedPrimaryRoleMeasurement) -> list[reporting.Field]:
    return [
        reporting.field(
            "tank",
            format_role_stats(
                measurement.tank_unit_max_hp,
                measurement.tank_unit_max_mana_points,
                measurement.tank_unit_move_points,
                measurement.tank_physical_damage_received_percent,
                measurement.tank_magic_damage_received_percent,
            ),
        ),
        reporting.field(
            "damage",
            format_role_stats(
                measurement.damage_unit_max_hp,
                measurement.damage_unit_max_mana_points,
                measurement.damage_unit_move_points,
                measurement.damage_physical_damage_received_percent,
                measurement.damage_magic_damage_received_percent,
            ),
        ),
        reporting.field(
            "healer",
            format_role_stats(
                measurement.healer_unit_max_hp,
                measurement.healer_unit_max_mana_points,
                measurement.healer_unit_move_points,
                measurement.healer_physical_damage_received_percent,
                measurement.healer_magic_damage_received_percent,
            ),
        ),
        reporting.field("turn-limit-rate", measurement.turn_limit_rate, ".2%"),
        reporting.field("avg-attacker-turns", measurement.average_attacker_turn_count, ".2f"),
        reporting.field("tank-score", measurement.tank_primary_role_score, ".4f"),
        reporting.field("damage-score", measurement.damage_primary_role_score, ".4f"),
        reporting.field("healer-score", measurement.healer_primary_role_score, ".4f"),
        reporting.field("role-score", measurement.average_primary_role_score, ".4f"),
        reporting.field("fitness", measurement.fitness, ".4f"),
    ]


def format_role_stats(hp: int, mana: int, move: int, phys_dr: int, magic_dr: int) -> str:
    return f"hp:{hp},mana:{mana},move:{move},phys:{phys_dr},magic:{magic_dr}"


def print_candidate_measurement(
    measurement: measurements.CombinedPrimaryRoleMeasurement,
    elapsed_seconds: float,
    cached: bool,
) -> None:
    reporting.print_record(
        "candidate",
        [
            reporting.field("elapsed", elapsed_seconds, ".1f"),
            reporting.field("cached", str(cached).lower()),
            *combined_primary_role_fields(measurement),
        ],
    )
