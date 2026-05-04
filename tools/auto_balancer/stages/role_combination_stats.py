#!/usr/bin/env python3
"""Balance all 9 primary×secondary role combinations in coordinated rounds.

Each round visits all 9 combinations in order. Within a round every combination
sees the most recent stats written by the previous combination, so the tradeoff
scoring always has fresh peer data. Multiple rounds let each combination react to
changes made by others until the system converges.

Run order per round mirrors the dependency structure: tanks first (highest HP
anchors the acrobat ratio comparisons), then healers, then damage.
"""
from __future__ import annotations

import argparse
import random
from dataclasses import dataclass
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow
from auto_balancer.workflows import secondary_roles as secondary_role_balancer


# (primary_role, secondary_role, balance_section_name)
COMBINATION_CONFIGS: tuple[tuple[str, str, str], ...] = (
    ("Tank", "Buffer", "tank_buffer"),
    ("Tank", "Debuffer", "tank_debuffer"),
    ("Tank", "Acrobat", "tank_acrobat"),
    ("Healer", "Buffer", "healer_buffer"),
    ("Healer", "Debuffer", "healer_debuffer"),
    ("Healer", "Acrobat", "healer_acrobat"),
    ("Damage", "Buffer", "damage_buffer"),
    ("Damage", "Debuffer", "damage_debuffer"),
    ("Damage", "Acrobat", "damage_acrobat"),
)

SECONDARY_ROLE_ORDER = ("Buffer", "Debuffer", "Acrobat")
STATS_PER_COMBINATION = 5


@dataclass(frozen=True)
class CombinationWorkItem:
    primary_role: str
    secondary_role: str
    balance_section_name: str
    config: config_models.SecondaryRoleBalancerConfig
    initial: tuple[int, int, int, int, int]
    bounds: secondary_role_balancer.StatBounds


@dataclass(frozen=True)
class SecondaryFamilyMeasurement:
    secondary_role: str
    measurements_by_combination: dict[str, object]
    fitness: float


def load_balancer_config(args: argparse.Namespace) -> config_models.NestedCombinationBalancerConfig:
    return load_balancer_config_from_args(
        config_models.NestedCombinationBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def build_combination_config(
    nested_config: config_models.NestedCombinationBalancerConfig,
    primary_role: str,
    secondary_role: str,
    balance_section_name: str,
    round_index: int,
    combo_index: int,
) -> config_models.SecondaryRoleBalancerConfig:
    combination_seed = (
        nested_config.ga.ga_random_seed
        + (round_index * nested_config.ga.random_seed_step_per_round)
        + (combo_index * nested_config.ga.random_seed_step_per_combination)
    )
    ga_config = config_models.GaConfig(
        ga_random_seed=combination_seed,
        candidate_population_size=nested_config.ga.candidate_population_size,
        generation_count=nested_config.ga.generation_count,
        mutation_probability=nested_config.ga.mutation_probability,
        evaluation_turn_budget=nested_config.ga.evaluation_turn_budget,
        evaluation_repeat_stages=nested_config.ga.evaluation_repeat_stages,
        evaluation_timeout_seconds=nested_config.ga.evaluation_timeout_seconds,
        evaluation_log_mode=nested_config.ga.evaluation_log_mode,
        mcts_iteration_budget=nested_config.ga.mcts_iteration_budget,
    )
    balance_config = getattr(nested_config.balance, balance_section_name)
    if balance_config.target_primary_role != primary_role:
        raise ValueError(
            f"Combination config {balance_section_name!r} target_primary_role must be {primary_role!r}, "
            f"got {balance_config.target_primary_role!r}."
        )
    if balance_config.target_secondary_role != secondary_role:
        raise ValueError(
            f"Combination config {balance_section_name!r} target_secondary_role must be {secondary_role!r}, "
            f"got {balance_config.target_secondary_role!r}."
        )

    return config_models.SecondaryRoleBalancerConfig(
        scenario=nested_config.scenario,
        ga=ga_config,
        balance=balance_config,
    )


def validate_nested_config(nested_config: config_models.NestedCombinationBalancerConfig) -> None:
    if nested_config.balance.optimization_round_count <= 0:
        raise ValueError("Nested combination balancer optimization_round_count must be positive.")
    if not 0.0 <= nested_config.ga.crossover_probability <= 1.0:
        raise ValueError("Nested combination balancer crossover_probability must be between 0.0 and 1.0.")
    for combo_index, (primary_role, secondary_role, balance_section_name) in enumerate(COMBINATION_CONFIGS):
        combo_config = build_combination_config(
            nested_config,
            primary_role,
            secondary_role,
            balance_section_name,
            round_index=0,
            combo_index=combo_index,
        )
        secondary_role_balancer.validate_config(combo_config)


def get_secondary_family_configs(secondary_role: str) -> tuple[tuple[str, str, str], ...]:
    return tuple(config for config in COMBINATION_CONFIGS if config[1] == secondary_role)


def combination_key(primary_role: str, secondary_role: str) -> str:
    return f"{primary_role}+{secondary_role}"


def build_family_work_items(
    nested_config: config_models.NestedCombinationBalancerConfig,
    content_path: Path,
    secondary_role: str,
    round_index: int,
) -> list[CombinationWorkItem]:
    work_items: list[CombinationWorkItem] = []
    for combo_index, (primary_role, _, balance_section_name) in enumerate(get_secondary_family_configs(secondary_role)):
        config = build_combination_config(
            nested_config,
            primary_role,
            secondary_role,
            balance_section_name,
            round_index=round_index,
            combo_index=combo_index,
        )
        initial = secondary_role_balancer.load_initial_candidate(config, content_path)
        bounds = secondary_role_balancer.compute_stat_bounds(config, initial)
        work_items.append(CombinationWorkItem(primary_role, secondary_role, balance_section_name, config, initial, bounds))
    return work_items


def flatten_candidates(candidates: list[tuple[int, int, int, int, int]]) -> tuple[int, ...]:
    return tuple(value for candidate in candidates for value in candidate)


def split_family_candidate(candidate: tuple[int, ...]) -> list[tuple[int, int, int, int, int]]:
    return [
        (
            candidate[index],
            candidate[index + 1],
            candidate[index + 2],
            candidate[index + 3],
            candidate[index + 4],
        )
        for index in range(0, len(candidate), STATS_PER_COMBINATION)
    ]


def normalize_family_candidate(
    candidate: tuple[int, ...],
    work_items: list[CombinationWorkItem],
) -> tuple[int, ...]:
    return flatten_candidates([
        secondary_role_balancer.normalize_candidate(item.bounds, stat_candidate)
        for item, stat_candidate in zip(work_items, split_family_candidate(candidate), strict=True)
    ])


def apply_family_candidate(
    content_path: Path,
    work_items: list[CombinationWorkItem],
    candidate: tuple[int, ...],
) -> None:
    for item, stat_candidate in zip(work_items, split_family_candidate(candidate), strict=True):
        secondary_role_balancer.apply_candidate_to_content(item.config, content_path, stat_candidate)


def evaluate_family_candidate(
    content_path: Path,
    work_items: list[CombinationWorkItem],
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    candidate: tuple[int, ...],
) -> SecondaryFamilyMeasurement:
    normalized = normalize_family_candidate(candidate, work_items)
    apply_family_candidate(content_path, work_items, normalized)
    first_config = work_items[0].config
    summary = secondary_role_balancer.run_eval_role_alignment_with_stages(
        eval_config,
        first_config.ga.evaluation_turn_budget,
        first_config.ga.evaluation_repeat_stages,
        offensive_ability_ids,
    )

    measurements_by_combination: dict[str, object] = {}
    for item, stat_candidate in zip(work_items, split_family_candidate(normalized), strict=True):
        key = combination_key(item.primary_role, item.secondary_role)
        measurements_by_combination[key] = secondary_role_balancer.build_measurement(
            item.config,
            content_path,
            summary,
            stat_candidate,
        )

    fitness = sum(float(measurement.fitness) for measurement in measurements_by_combination.values()) / len(measurements_by_combination)
    return SecondaryFamilyMeasurement(work_items[0].secondary_role, measurements_by_combination, fitness)


class SecondaryFamilyWorkflow(CandidateWorkflow[tuple[int, ...], SecondaryFamilyMeasurement]):
    def __init__(
        self,
        *,
        secondary_role: str,
        round_index: int,
        work_items: list[CombinationWorkItem],
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
        crossover_probability: float,
    ):
        first_config = work_items[0].config
        self.creator_name_prefix = f"RoleCombination{secondary_role}Round{round_index + 1}"
        self.random_seed = first_config.ga.ga_random_seed
        self.population_size = first_config.ga.candidate_population_size
        self.generation_count = first_config.ga.generation_count
        self.mutation_probability = first_config.ga.mutation_probability
        self.crossover_probability = crossover_probability
        self.secondary_role = secondary_role
        self.work_items = work_items
        self.content_path = content_path
        self.eval_config = eval_config
        self.offensive_ability_ids = offensive_ability_ids
        self.initial_candidate = flatten_candidates([item.initial for item in work_items])

    def normalize_individual(self, individual: list[int]) -> tuple[int, ...]:
        return normalize_family_candidate(tuple(int(value) for value in individual), self.work_items)

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        normalized_seed = self.normalize_individual(list(self.initial_candidate))
        population = [individual_type(list(normalized_seed))]
        while len(population) < self.population_size:
            values: list[int] = []
            for item in self.work_items:
                values.extend([
                    rng.randint(*item.bounds.hp),
                    rng.randint(*item.bounds.mana),
                    rng.randint(*item.bounds.move),
                    rng.randint(*item.bounds.phys_dr),
                    rng.randint(*item.bounds.magic_dr),
                ])
            population.append(individual_type(list(self.normalize_individual(values))))
        return population[: self.population_size]

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        for combo_index, item in enumerate(self.work_items):
            offset = combo_index * STATS_PER_COMBINATION
            stat_bounds = [item.bounds.hp, item.bounds.mana, item.bounds.move, item.bounds.phys_dr, item.bounds.magic_dr]
            for stat_index, (lo, hi) in enumerate(stat_bounds):
                if rng.random() < 0.6:
                    span = max(1, (hi - lo) // 8)
                    individual[offset + stat_index] = ga.bounded_integer(
                        individual[offset + stat_index] + rng.randint(-span, span),
                        lo,
                        hi,
                    )
                elif rng.random() < 0.3:
                    individual[offset + stat_index] = rng.randint(lo, hi)
        return (individual,)

    def evaluate_candidate(self, candidate: tuple[int, ...]) -> SecondaryFamilyMeasurement:
        return evaluate_family_candidate(
            self.content_path,
            self.work_items,
            self.eval_config,
            self.offensive_ability_ids,
            candidate,
        )

    def get_fitness(self, measurement: SecondaryFamilyMeasurement) -> float:
        return measurement.fitness

    def on_candidate(self, measurement: SecondaryFamilyMeasurement, elapsed_seconds: float, cached: bool) -> None:
        reporting.print_record(
            "candidate-family",
            [
                reporting.field("secondary-role", measurement.secondary_role),
                reporting.field("fitness", measurement.fitness, ".4f"),
            ],
        )
        for key, combo_measurement in measurement.measurements_by_combination.items():
            reporting.print_record(f"candidate {key}", reporting.secondary_role_round_fields(combo_measurement))
        print(f"candidate-time elapsed={elapsed_seconds:.1f}s cached={str(cached).lower()}", flush=True)

    def on_generation_best(self, generation: int, measurement: SecondaryFamilyMeasurement) -> None:
        reporting.print_record(
            f"generation {generation} best-family",
            [
                reporting.field("secondary-role", measurement.secondary_role),
                reporting.field("fitness", measurement.fitness, ".4f"),
            ],
        )


def optimize_secondary_family(
    nested_config: config_models.NestedCombinationBalancerConfig,
    secondary_role: str,
    round_index: int,
    work_items: list[CombinationWorkItem],
    content_path: Path,
    offensive_ability_ids: set[str],
) -> SecondaryFamilyMeasurement:
    eval_config = secondary_role_balancer.build_eval_config(work_items[0].config, content_path)
    workflow = SecondaryFamilyWorkflow(
        secondary_role=secondary_role,
        round_index=round_index,
        work_items=work_items,
        content_path=content_path,
        eval_config=eval_config,
        offensive_ability_ids=offensive_ability_ids,
        crossover_probability=nested_config.ga.crossover_probability,
    )
    best_key, best_measurement = run_candidate_workflow(workflow)
    apply_family_candidate(content_path, work_items, best_key)
    return best_measurement


def run(
    nested_config: config_models.NestedCombinationBalancerConfig,
    source_content_path: Path | None = None,
    output_package_path: Path | None = None,
    persist_results: bool = True,
) -> int:
    runtime.ensure_deap_available()
    validate_nested_config(nested_config)
    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path

    # All combinations share one content pack so tradeoff scoring always sees
    # the full roster. Prepare content once using the first combination's config.
    first_primary, first_secondary, first_section = COMBINATION_CONFIGS[0]
    first_combo_config = build_combination_config(
        nested_config,
        first_primary,
        first_secondary,
        first_section,
        round_index=0,
        combo_index=0,
    )
    content_path = secondary_role_balancer.prepare_eval_content(first_combo_config, content_source)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)

    best_by_combination: dict[str, object] = {}
    before_by_combination: dict[str, object] = {}
    round_count = nested_config.balance.optimization_round_count

    for round_index in range(round_count):
        print(f"round {round_index + 1}/{round_count} start", flush=True)

        for secondary_role in SECONDARY_ROLE_ORDER:
            work_items = build_family_work_items(nested_config, content_path, secondary_role, round_index)
            family_keys = [combination_key(item.primary_role, item.secondary_role) for item in work_items]
            print(
                f"optimizing {secondary_role} family "
                f"(round={round_index + 1}, "
                f"combinations={','.join(family_keys)}, "
                f"pop={work_items[0].config.ga.candidate_population_size}, "
                f"gens={work_items[0].config.ga.generation_count}, "
                f"repeat={work_items[0].config.ga.evaluation_repeat_stages[-1].total_repeats}, "
                f"turns={work_items[0].config.ga.evaluation_turn_budget})",
                flush=True,
            )

            eval_config = secondary_role_balancer.build_eval_config(work_items[0].config, content_path)
            initial_candidate = flatten_candidates([item.initial for item in work_items])
            before_measurement = evaluate_family_candidate(
                content_path,
                work_items,
                eval_config,
                offensive_ability_ids,
                initial_candidate,
            )
            for key, measurement in before_measurement.measurements_by_combination.items():
                before_by_combination.setdefault(key, measurement)

            best_family = optimize_secondary_family(
                nested_config,
                secondary_role,
                round_index,
                work_items,
                content_path,
                offensive_ability_ids,
            )
            for key, measurement in best_family.measurements_by_combination.items():
                best_by_combination[key] = measurement
                reporting.print_record(
                    f"round-best {key}",
                    reporting.secondary_role_round_fields(measurement),
                )

    print("nested combination balancing complete", flush=True)
    for primary_role, secondary_role, _ in COMBINATION_CONFIGS:
        key = combination_key(primary_role, secondary_role)
        measurement = best_by_combination.get(key)
        if measurement is None:
            continue
        reporting.print_record(f"best {key}", reporting.secondary_role_round_fields(measurement))

    if output_package_path is not None:
        balance_package.write_balance_package(
            output_package_path,
            "role-combination-stats",
            content_source,
            content_path,
            build_package_report(before_by_combination, best_by_combination),
            changed_files=("unitTemplates.json",),
        )
        print(f"package={output_package_path}", flush=True)

    if persist_results:
        # Persist the tuned unit stats back to the source content directory so
        # legacy runs still behave as before. Pipeline package runs disable this.
        scenarios.save_file_to_source_content(
            content_path,
            content_source,
            "unitTemplates.json",
        )
        print(f"saved unitTemplates.json to {content_source}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0


def build_package_report(before_by_combination: dict[str, object], best_by_combination: dict[str, object]) -> dict:
    return reporting.build_evidence_report(
        before_by_combination,
        best_by_combination,
        (
            ("Fitness", "fitness"),
            ("AttackerWinRate", "attacker_win_rate"),
            ("RoleCombinationWinRate", "role_combination_win_rate"),
            ("RoleCombinationScore", "role_combination_score"),
            ("TurnLimitRate", "turn_limit_rate"),
        ),
    )
