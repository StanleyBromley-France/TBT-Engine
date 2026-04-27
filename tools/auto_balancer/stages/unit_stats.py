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
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.config import load_balancer_config_from_args
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

        for combo_index, (primary_role, secondary_role, balance_section_name) in enumerate(COMBINATION_CONFIGS):
            combo_config = build_combination_config(
                nested_config,
                primary_role,
                secondary_role,
                balance_section_name,
                round_index=round_index,
                combo_index=combo_index,
            )
            combination_key = f"{primary_role}+{secondary_role}"
            print(
                f"optimizing {combination_key} "
                f"(round={round_index + 1}, "
                f"pop={combo_config.ga.candidate_population_size}, "
                f"gens={combo_config.ga.generation_count}, "
                f"repeat={combo_config.ga.evaluation_repeat_stages[-1].total_repeats}, "
                f"turns={combo_config.ga.evaluation_turn_budget})",
                flush=True,
            )

            eval_config = secondary_role_balancer.build_eval_config(combo_config, content_path)
            if combination_key not in before_by_combination:
                initial_candidate = secondary_role_balancer.load_initial_candidate(combo_config, content_path)
                initial_bounds = secondary_role_balancer.compute_stat_bounds(combo_config, initial_candidate)
                before_by_combination[combination_key] = secondary_role_balancer.evaluate_candidate(
                    combo_config,
                    content_path,
                    eval_config,
                    offensive_ability_ids,
                    initial_bounds,
                    initial_candidate,
                )
            best_measurement = secondary_role_balancer.optimize_secondary_role(
                combo_config,
                content_path,
                eval_config,
                offensive_ability_ids,
            )
            best_by_combination[combination_key] = best_measurement
            reporting.print_record(
                f"round-best {combination_key}",
                reporting.secondary_role_round_fields(best_measurement),
            )

    print("nested combination balancing complete", flush=True)
    for primary_role, secondary_role, _ in COMBINATION_CONFIGS:
        combination_key = f"{primary_role}+{secondary_role}"
        measurement = best_by_combination.get(combination_key)
        if measurement is None:
            continue
        reporting.print_record(f"best {combination_key}", reporting.secondary_role_round_fields(measurement))

    if output_package_path is not None:
        balance_package.write_balance_package(
            output_package_path,
            "unit-stats",
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
            ("PrimaryRoleScore", "primary_role_value_score"),
            ("SecondaryRoleScore", "secondary_role_alignment_score"),
            ("TurnLimitRate", "turn_limit_rate"),
        ),
    )
