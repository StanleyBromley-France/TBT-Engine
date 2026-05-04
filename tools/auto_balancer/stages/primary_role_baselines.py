#!/usr/bin/env python3
"""Run primary role tuning across several roles and rounds."""
from __future__ import annotations

import argparse

import auto_balancer.config_models as config_models
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.workflows import combined_primary_roles
from auto_balancer.workflows import primary_roles as primary_role_balancer


PRIMARY_ROLE_CONFIGS: tuple[tuple[str, str], ...] = (
    ("Tank", "tank"),
    ("Damage", "damage"),
    ("Healer", "healer"),
)


def load_balancer_config(args: argparse.Namespace) -> config_models.NestedPrimaryRoleBalancerConfig:
    return load_balancer_config_from_args(
        config_models.NestedPrimaryRoleBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def build_primary_role_config(
    config: config_models.NestedPrimaryRoleBalancerConfig,
    role_name: str,
    balance_section_name: str,
    role_seed: int | None = None,
) -> config_models.PrimaryRoleBalancerConfig:
    ga_config = config_models.GaConfig(
        ga_random_seed=config.ga.ga_random_seed if role_seed is None else role_seed,
        candidate_population_size=config.ga.candidate_population_size,
        generation_count=config.ga.generation_count,
        mutation_probability=config.ga.mutation_probability,
        evaluation_turn_budget=config.ga.evaluation_turn_budget,
        evaluation_repeat_stages=config.ga.evaluation_repeat_stages,
        evaluation_timeout_seconds=config.ga.evaluation_timeout_seconds,
        evaluation_log_mode=config.ga.evaluation_log_mode,
        mcts_iteration_budget=config.ga.mcts_iteration_budget,
    )
    balance_config = getattr(config.balance, balance_section_name)
    if balance_config.target_primary_role != role_name:
        raise ValueError(
            f"Nested primary role {balance_section_name} config target_primary_role must be {role_name!r}."
        )

    return config_models.PrimaryRoleBalancerConfig(
        scenario=config.scenario,
        ga=ga_config,
        balance=balance_config,
    )


def validate_nested_config(config: config_models.NestedPrimaryRoleBalancerConfig) -> None:
    if config.balance.optimization_round_count <= 0:
        raise ValueError("Nested primary role balancer optimization_round_count must be positive.")
    if not 0.0 <= config.ga.crossover_probability <= 1.0:
        raise ValueError("Nested primary role balancer crossover_probability must be between 0.0 and 1.0.")
    for role_name, balance_section_name in PRIMARY_ROLE_CONFIGS:
        primary_role_balancer.validate_config(build_primary_role_config(config, role_name, balance_section_name))


def derive_role_config(
    nested_config: config_models.NestedPrimaryRoleBalancerConfig,
    role_name: str,
    balance_section_name: str,
    round_index: int,
    role_index: int,
) -> config_models.PrimaryRoleBalancerConfig:
    role_seed = (
        nested_config.ga.ga_random_seed
        + (round_index * nested_config.ga.random_seed_step_per_round)
        + (role_index * nested_config.ga.random_seed_step_per_role)
    )
    return build_primary_role_config(nested_config, role_name, balance_section_name, role_seed=role_seed)


def run(
    nested_config: config_models.NestedPrimaryRoleBalancerConfig,
    source_content_path=None,
    output_package_path=None,
    persist_results: bool = False,
) -> int:
    runtime.ensure_deap_available()
    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path

    # All target roles share one generated content pack so each role pass builds on the previous one.
    validate_nested_config(nested_config)

    base_role_name, base_balance_section_name = PRIMARY_ROLE_CONFIGS[0]
    base_config = build_primary_role_config(nested_config, base_role_name, base_balance_section_name)
    content_path = primary_role_balancer.prepare_eval_content(base_config, content_source)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    eval_config = primary_role_balancer.build_eval_config(base_config, content_path)
    role_specs = combined_primary_roles.build_role_specs(
        nested_config,
        build_primary_role_config,
        content_path,
    )

    reporting.print_record(
        "optimising primary roles",
        [
            reporting.field("population", nested_config.ga.candidate_population_size),
            reporting.field("generations", nested_config.ga.generation_count),
            reporting.field("repeat", nested_config.ga.evaluation_repeat_stages[-1].total_repeats),
            reporting.field("turn-count", nested_config.ga.evaluation_turn_budget),
        ],
    )
    before = combined_primary_roles.evaluate_current_content(
        nested_config,
        role_specs,
        content_path,
        eval_config,
        offensive_ability_ids,
    )
    best = combined_primary_roles.optimize_combined_primary_roles(
        nested_config,
        role_specs,
        content_path,
        eval_config,
        offensive_ability_ids,
    )

    reporting.print_banner("primary role baseline complete", combined_primary_roles.combined_primary_role_fields(best))
    if output_package_path is not None:
        balance_package.write_balance_package(
            output_package_path,
            "primary-role-baselines",
            content_source,
            content_path,
            build_package_report(before, best),
            changed_files=("unitTemplates.json",),
        )
        print(f"package={output_package_path}", flush=True)

    if persist_results:
        scenarios.save_file_to_source_content(
            content_path,
            content_source,
            "unitTemplates.json",
        )
        print(f"saved unitTemplates.json to {content_source}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0


def build_package_report(before: object, best: object) -> dict:
    return reporting.build_evidence_report(
        {"primary-roles": before},
        {"primary-roles": best},
        (
            ("Fitness", "fitness"),
            ("AttackerWinRate", "attacker_win_rate"),
            ("AverageAttackerTurns", "average_attacker_turn_count"),
            ("AveragePrimaryRoleScore", "average_primary_role_score"),
            ("TankPrimaryRoleScore", "tank_primary_role_score"),
            ("DamagePrimaryRoleScore", "damage_primary_role_score"),
            ("HealerPrimaryRoleScore", "healer_primary_role_score"),
            ("TurnLimitRate", "turn_limit_rate"),
        ),
    )
