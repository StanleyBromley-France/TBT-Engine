#!/usr/bin/env python3
"""Full-genome balancing workflow helpers.

This module treats the genome as modifiers over the authored content:

* five unit-stat modifier genes for each configured unit profile
* one global multiplier gene for each configured ability-effect group
"""
from __future__ import annotations

import random
import statistics
from pathlib import Path
from types import SimpleNamespace

import auto_balancer.eval as eval_api
import auto_balancer.config_models as config_models
import auto_balancer.ga as ga
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.scenarios import content as scenario_content
from auto_balancer.config_models.full_genome_config import UnitStatModifierBounds
from balancing_scripts.primary_roles.common import mean
from balancing_scripts.primary_roles.scoring import compute_primary_role_score
from auto_balancer.measurement_models.full_genome_measurement import (
    AbilityGroupMultiplierMap,
    FullGenomeCandidate,
    FullGenomeMeasurement,
    UnitProfileModifierMap,
)
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow
from auto_balancer.workflows import grouped_ability_effects
from auto_balancer.workflows import role_stats


UnitProfileGenes = tuple[int, int, int, int, int]

EXPECTED_UNIT_GENE_ORDER: tuple[str, ...] = (
    "max_hp_multiplier_percent",
    "max_mana_points_multiplier_percent",
    "move_points_additive_delta",
    "physical_damage_received_additive_delta",
    "magic_damage_received_additive_delta",
)

COMPONENT_VALUE_GROUPS = frozenset(
    {
        "tank_damage_percent",
        "healer_healing_percent",
        "damage_damage_percent",
    }
)
MODIFIER_VALUE_GROUPS = frozenset(
    {
        "buffer_modifier_percent",
        "debuffer_modifier_percent",
    }
)
MANA_COST_GROUP = "mana_cost_percent"


class FullGenomeWorkflow(CandidateWorkflow[FullGenomeCandidate, FullGenomeMeasurement]):
    def __init__(
        self,
        config: config_models.FullGenomeBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
        baseline_unit_templates: list[dict],
        grouped_ability_index: grouped_ability_effects.GroupedAbilityIndex,
    ):
        self.creator_name_prefix = "FullGenome"
        self.random_seed = config.ga.ga_random_seed
        self.population_size = config.ga.candidate_population_size
        self.generation_count = config.ga.generation_count
        self.mutation_probability = config.ga.mutation_probability
        self.crossover_probability = config.ga.crossover_probability
        self.config = config
        self.content_path = content_path
        self.eval_config = eval_config
        self.offensive_ability_ids = offensive_ability_ids
        self.baseline_unit_templates = baseline_unit_templates
        self.grouped_ability_index = grouped_ability_index
        self.neutral_candidate = build_neutral_candidate(config)
        self.interrupted = False

    def normalize_individual(self, individual: list[int]) -> FullGenomeCandidate:
        return normalize_candidate(self.config, tuple(int(value) for value in individual))

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        population: list = [individual_type(list(self.neutral_candidate))]
        seen: set[FullGenomeCandidate] = {self.neutral_candidate}
        while len(population) < self.population_size:
            candidate = build_random_candidate(self.config, rng)
            if candidate in seen:
                continue
            seen.add(candidate)
            population.append(individual_type(list(candidate)))
        return population[: self.population_size]

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        unit_modifiers, ability_multipliers = split_candidate(self.config, tuple(int(value) for value in individual))
        mutation_config = self.config.balance.mutation

        for profile_name, genes in unit_modifiers.items():
            if rng.random() <= mutation_config.profile_block_mutation_probability:
                bounds = get_unit_bounds_for_profile(self.config, profile_name)
                unit_modifiers[profile_name] = (
                    rng.randint(*bounds.max_hp_multiplier_percent),
                    rng.randint(*bounds.max_mana_points_multiplier_percent),
                    rng.randint(*bounds.move_points_additive_delta),
                    rng.randint(*bounds.physical_damage_received_additive_delta),
                    rng.randint(*bounds.magic_damage_received_additive_delta),
                )
                continue
            unit_modifiers[profile_name] = mutate_unit_profile_genes(self.config, profile_name, genes, rng)

        for group_name, multiplier in ability_multipliers.items():
            if rng.random() <= mutation_config.ability_group_gene_probability:
                ability_multipliers[group_name] = mutate_ability_group_multiplier(self.config, group_name, multiplier, rng)

        mutated = flatten_candidate(self.config, unit_modifiers, ability_multipliers)
        for index, value in enumerate(mutated):
            individual[index] = value
        return (individual,)

    def evaluate_candidate(self, candidate: FullGenomeCandidate) -> FullGenomeMeasurement:
        return evaluate_candidate(
            self.config,
            self.content_path,
            self.eval_config,
            self.offensive_ability_ids,
            candidate,
            baseline_unit_templates=self.baseline_unit_templates,
            grouped_ability_index=self.grouped_ability_index,
        )

    def get_fitness(self, measurement: FullGenomeMeasurement) -> float:
        return measurement.fitness

    def on_candidate(self, measurement: FullGenomeMeasurement, elapsed_seconds: float, cached: bool) -> None:
        reporting.print_record(
            "candidate",
            [
                reporting.field("elapsed", elapsed_seconds, ".1f"),
                reporting.field("cached", str(cached).lower()),
                *full_genome_summary_fields(measurement, detailed=True),
            ],
        )

    def on_generation_best(self, generation: int, measurement: FullGenomeMeasurement) -> None:
        reporting.print_section(
            f"generation {generation} best",
            full_genome_summary_fields(measurement, detailed=True),
        )

    def on_interrupted_best(self, candidate: FullGenomeCandidate, measurement: FullGenomeMeasurement) -> None:
        self.interrupted = True
        reporting.print_section(
            "interrupted; using best candidate so far",
            full_genome_summary_fields(measurement, detailed=True),
        )


def validate_candidate_layout(config: config_models.FullGenomeBalancerConfig) -> None:
    balance = config.balance
    if balance.genome.model != "role_profile_modifiers_plus_ability_multipliers":
        raise ValueError(f"Unsupported full-genome model: {balance.genome.model!r}.")
    if balance.genome.unit_stat_profile_modifiers.gene_order != EXPECTED_UNIT_GENE_ORDER:
        raise ValueError(
            "Full-genome unit gene order must be "
            + ", ".join(EXPECTED_UNIT_GENE_ORDER)
        )

    unknown_groups = [
        group
        for group in balance.genome.ability_effect_groups.group_order
        if group not in COMPONENT_VALUE_GROUPS
        and group not in MODIFIER_VALUE_GROUPS
        and group != MANA_COST_GROUP
    ]
    if unknown_groups:
        raise ValueError("Full-genome ability group order contains unknown groups: " + ", ".join(unknown_groups))


def unit_profile_gene_count(config: config_models.FullGenomeBalancerConfig) -> int:
    return (
        len(config.balance.genome.unit_stat_profile_modifiers.profile_order)
        * len(config.balance.genome.unit_stat_profile_modifiers.gene_order)
    )


def ability_group_gene_count(config: config_models.FullGenomeBalancerConfig) -> int:
    return len(config.balance.genome.ability_effect_groups.group_order)


def expected_candidate_length(config: config_models.FullGenomeBalancerConfig) -> int:
    return unit_profile_gene_count(config) + ability_group_gene_count(config)


def build_neutral_candidate(config: config_models.FullGenomeBalancerConfig) -> FullGenomeCandidate:
    validate_candidate_layout(config)
    unit_genes: list[int] = []
    for _ in config.balance.genome.unit_stat_profile_modifiers.profile_order:
        unit_genes.extend([100, 100, 0, 0, 0])

    ability_genes = [100 for _ in config.balance.genome.ability_effect_groups.group_order]
    return tuple(unit_genes + ability_genes)


def split_candidate(
    config: config_models.FullGenomeBalancerConfig,
    candidate: tuple[int, ...],
) -> tuple[UnitProfileModifierMap, AbilityGroupMultiplierMap]:
    validate_candidate_length(config, candidate)

    unit_gene_names = config.balance.genome.unit_stat_profile_modifiers.gene_order
    profile_order = config.balance.genome.unit_stat_profile_modifiers.profile_order
    profile_gene_count = len(unit_gene_names)

    unit_modifiers: UnitProfileModifierMap = {}
    for profile_index, profile_name in enumerate(profile_order):
        start = profile_index * profile_gene_count
        end = start + profile_gene_count
        unit_modifiers[profile_name] = tuple(candidate[start:end])  # type: ignore[assignment]

    ability_start = unit_profile_gene_count(config)
    ability_multipliers = {
        group_name: int(candidate[ability_start + group_index])
        for group_index, group_name in enumerate(config.balance.genome.ability_effect_groups.group_order)
    }

    return unit_modifiers, ability_multipliers


def flatten_candidate(
    config: config_models.FullGenomeBalancerConfig,
    unit_modifiers: UnitProfileModifierMap,
    ability_multipliers: AbilityGroupMultiplierMap,
) -> FullGenomeCandidate:
    values: list[int] = []
    for profile_name in config.balance.genome.unit_stat_profile_modifiers.profile_order:
        values.extend(unit_modifiers[profile_name])
    for group_name in config.balance.genome.ability_effect_groups.group_order:
        values.append(ability_multipliers[group_name])
    return tuple(values)


def normalize_candidate(
    config: config_models.FullGenomeBalancerConfig,
    candidate: tuple[int, ...],
) -> FullGenomeCandidate:
    unit_modifiers, ability_multipliers = split_candidate(config, tuple(int(value) for value in candidate))

    normalized_unit_modifiers = {
        profile_name: normalize_unit_profile_genes(config, profile_name, genes)
        for profile_name, genes in unit_modifiers.items()
    }
    normalized_ability_multipliers = {
        group_name: normalize_ability_group_multiplier(config, group_name, multiplier)
        for group_name, multiplier in ability_multipliers.items()
    }
    return flatten_candidate(config, normalized_unit_modifiers, normalized_ability_multipliers)


def normalize_unit_profile_genes(
    config: config_models.FullGenomeBalancerConfig,
    profile_name: str,
    genes: UnitProfileGenes,
) -> UnitProfileGenes:
    bounds = get_unit_bounds_for_profile(config, profile_name)
    return (
        ga.bounded_integer(genes[0], *bounds.max_hp_multiplier_percent),
        ga.bounded_integer(genes[1], *bounds.max_mana_points_multiplier_percent),
        ga.bounded_integer(genes[2], *bounds.move_points_additive_delta),
        ga.bounded_integer(genes[3], *bounds.physical_damage_received_additive_delta),
        ga.bounded_integer(genes[4], *bounds.magic_damage_received_additive_delta),
    )


def normalize_ability_group_multiplier(
    config: config_models.FullGenomeBalancerConfig,
    group_name: str,
    multiplier: int,
) -> int:
    search_space = config.balance.search_space.ability_effect_groups
    if group_name in COMPONENT_VALUE_GROUPS:
        return ga.bounded_integer(multiplier, *search_space.component_value_multiplier_percent)
    if group_name in MODIFIER_VALUE_GROUPS:
        return ga.bounded_integer(multiplier, *search_space.modifier_value_multiplier_percent)
    if group_name == MANA_COST_GROUP:
        return ga.bounded_integer(multiplier, *search_space.mana_cost_multiplier_percent)
    raise ValueError(f"Unknown full-genome ability group: {group_name!r}.")


def get_unit_bounds_for_profile(
    config: config_models.FullGenomeBalancerConfig,
    profile_name: str,
) -> UnitStatModifierBounds:
    primary_role = profile_name.split("+", 1)[0]
    bounds_by_primary_role = config.balance.search_space.unit_stat_profile_modifiers.bounds_by_primary_role
    try:
        return getattr(bounds_by_primary_role, primary_role)
    except AttributeError as exc:
        raise ValueError(f"Unknown primary role in full-genome profile: {profile_name!r}.") from exc


def validate_candidate_length(
    config: config_models.FullGenomeBalancerConfig,
    candidate: tuple[int, ...],
) -> None:
    expected_length = expected_candidate_length(config)
    if len(candidate) != expected_length:
        raise ValueError(f"Expected {expected_length} full-genome genes, got {len(candidate)}.")


def build_random_candidate(
    config: config_models.FullGenomeBalancerConfig,
    rng: random.Random,
) -> FullGenomeCandidate:
    unit_modifiers: UnitProfileModifierMap = {}
    for profile_name in config.balance.genome.unit_stat_profile_modifiers.profile_order:
        bounds = get_unit_bounds_for_profile(config, profile_name)
        unit_modifiers[profile_name] = (
            rng.randint(*bounds.max_hp_multiplier_percent),
            rng.randint(*bounds.max_mana_points_multiplier_percent),
            rng.randint(*bounds.move_points_additive_delta),
            rng.randint(*bounds.physical_damage_received_additive_delta),
            rng.randint(*bounds.magic_damage_received_additive_delta),
        )

    ability_multipliers = {
        group_name: random_ability_group_multiplier(config, group_name, rng)
        for group_name in config.balance.genome.ability_effect_groups.group_order
    }
    return flatten_candidate(config, unit_modifiers, ability_multipliers)


def random_ability_group_multiplier(
    config: config_models.FullGenomeBalancerConfig,
    group_name: str,
    rng: random.Random,
) -> int:
    search_space = config.balance.search_space.ability_effect_groups
    if group_name in COMPONENT_VALUE_GROUPS:
        return rng.randint(*search_space.component_value_multiplier_percent)
    if group_name in MODIFIER_VALUE_GROUPS:
        return rng.randint(*search_space.modifier_value_multiplier_percent)
    if group_name == MANA_COST_GROUP:
        return rng.randint(*search_space.mana_cost_multiplier_percent)
    raise ValueError(f"Unknown full-genome ability group: {group_name!r}.")


def mutate_unit_profile_genes(
    config: config_models.FullGenomeBalancerConfig,
    profile_name: str,
    genes: UnitProfileGenes,
    rng: random.Random,
) -> UnitProfileGenes:
    mutation_config = config.balance.mutation
    bounds = get_unit_bounds_for_profile(config, profile_name)
    stat_bounds = (
        bounds.max_hp_multiplier_percent,
        bounds.max_mana_points_multiplier_percent,
        bounds.move_points_additive_delta,
        bounds.physical_damage_received_additive_delta,
        bounds.magic_damage_received_additive_delta,
    )
    mutated = list(genes)
    for index, (low, high) in enumerate(stat_bounds):
        if rng.random() > mutation_config.unit_profile_modifier_gene_probability:
            continue
        if rng.random() <= mutation_config.small_step_probability:
            step = max(1, (high - low) // mutation_config.stat_step_divisor)
            mutated[index] = ga.bounded_integer(mutated[index] + rng.randint(-step, step), low, high)
        elif rng.random() <= mutation_config.random_reset_probability:
            mutated[index] = rng.randint(low, high)
    return tuple(mutated)  # type: ignore[return-value]


def mutate_ability_group_multiplier(
    config: config_models.FullGenomeBalancerConfig,
    group_name: str,
    multiplier: int,
    rng: random.Random,
) -> int:
    low, high = get_ability_group_bounds(config, group_name)
    mutation_config = config.balance.mutation
    if rng.random() <= mutation_config.small_step_probability:
        step = max(1, (high - low) // mutation_config.ability_step_divisor)
        return ga.bounded_integer(multiplier + rng.randint(-step, step), low, high)
    if rng.random() <= mutation_config.random_reset_probability:
        return rng.randint(low, high)
    return multiplier


def get_ability_group_bounds(
    config: config_models.FullGenomeBalancerConfig,
    group_name: str,
) -> tuple[int, int]:
    search_space = config.balance.search_space.ability_effect_groups
    if group_name in COMPONENT_VALUE_GROUPS:
        return search_space.component_value_multiplier_percent
    if group_name in MODIFIER_VALUE_GROUPS:
        return search_space.modifier_value_multiplier_percent
    if group_name == MANA_COST_GROUP:
        return search_space.mana_cost_multiplier_percent
    raise ValueError(f"Unknown full-genome ability group: {group_name!r}.")


def optimize_full_genome(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    baseline_unit_templates: list[dict],
    grouped_ability_index: grouped_ability_effects.GroupedAbilityIndex,
) -> tuple[FullGenomeMeasurement, bool]:
    workflow = FullGenomeWorkflow(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        baseline_unit_templates,
        grouped_ability_index,
    )
    best_key, best_measurement = run_candidate_workflow(workflow)
    apply_candidate_to_content(
        config,
        content_path,
        best_key,
        baseline_unit_templates=baseline_unit_templates,
        grouped_ability_index=grouped_ability_index,
    )
    return best_measurement, workflow.interrupted


def run(
    config: config_models.FullGenomeBalancerConfig,
    *,
    source_content_path: Path | None = None,
    output_package_path: Path | None = None,
    persist_results: bool = False,
) -> int:
    runtime.ensure_deap_available()
    validate_config(config)

    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path
    content_path = prepare_eval_content(config, content_source)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    baseline_unit_templates = scenario_content.load_unit_templates(content_path)
    grouped_ability_index = grouped_ability_effects.build_grouped_index(content_path)

    reporting.print_record(
        "optimising full genome",
        [
            reporting.field("genes", expected_candidate_length(config)),
            reporting.field("population", config.ga.candidate_population_size),
            reporting.field("generations", config.ga.generation_count),
            reporting.field("scenarios", config.scenario.generated_scenario_count),
            reporting.field("repeat", config.ga.evaluation_repeat_stages[-1].total_repeats),
            reporting.field("turns", config.ga.evaluation_turn_budget),
        ],
    )

    try:
        before = evaluate_current_content(config, content_path, eval_config, offensive_ability_ids)
        best, interrupted = optimize_full_genome(
            config,
            content_path,
            eval_config,
            offensive_ability_ids,
            baseline_unit_templates,
            grouped_ability_index,
        )
    except KeyboardInterrupt:
        print("interrupted; no completed candidate report was available", flush=True)
        print(f"content={content_path}", flush=True)
        return 130
    reporting.print_banner("full genome complete", full_genome_summary_fields(best, detailed=True))

    if output_package_path is not None:
        balance_package.write_balance_package(
            output_package_path,
            "full-genome",
            content_source,
            content_path,
            build_package_report(before, best),
            changed_files=("unitTemplates.json", "effectComponentTemplates.json", "abilities.json"),
        )
        print(f"package={output_package_path}", flush=True)

    if persist_results:
        scenarios.save_file_to_source_content(content_path, content_source, "unitTemplates.json")
        scenarios.save_file_to_source_content(content_path, content_source, "effectComponentTemplates.json")
        scenarios.save_file_to_source_content(content_path, content_source, "abilities.json")
        print(f"saved full-genome tuned content to {content_source}", flush=True)

    print(f"content={content_path}", flush=True)
    return 130 if interrupted else 0


def validate_config(config: config_models.FullGenomeBalancerConfig) -> None:
    validate_candidate_layout(config)
    if config.ga.candidate_population_size <= 0:
        raise ValueError("Full-genome candidate_population_size must be positive.")
    if config.ga.generation_count < 0:
        raise ValueError("Full-genome generation_count must be zero or greater.")
    if not 0.0 <= config.ga.mutation_probability <= 1.0:
        raise ValueError("Full-genome mutation_probability must be between 0.0 and 1.0.")
    if not 0.0 <= config.ga.crossover_probability <= 1.0:
        raise ValueError("Full-genome crossover_probability must be between 0.0 and 1.0.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Full-genome generated_scenario_count must be positive.")
    weight_sum = sum(config.balance.fitness_weights.__dict__.values())
    if abs(weight_sum - 1.0) > 1e-6:
        raise ValueError(f"Full-genome fitness weights must sum to 1.0, got {weight_sum:.6f}.")


def full_genome_summary_fields(measurement: FullGenomeMeasurement, *, detailed: bool) -> list[reporting.Field]:
    fields = [
        reporting.field("fitness", measurement.fitness, ".4f"),
        reporting.field("winrate", measurement.attacker_win_rate, ".2%"),
        reporting.field("turn-limit", measurement.turn_limit_rate, ".2%"),
        reporting.field("avg-attacker-turns", measurement.average_attacker_turn_count, ".2f"),
    ]
    if detailed:
        fields.extend(
            [
                reporting.field("match", measurement.match_flow_score, ".4f"),
                reporting.field("primary", measurement.primary_role_identity_score, ".4f"),
                reporting.field("secondary", measurement.secondary_role_identity_score, ".4f"),
                reporting.field("fairness", measurement.role_profile_fairness_score, ".4f"),
                reporting.field("change-shape", measurement.change_shape_score, ".4f"),
            ]
        )
    return fields


def build_package_report(before: FullGenomeMeasurement, after: FullGenomeMeasurement) -> dict:
    return reporting.build_evidence_report(
        {"full-genome": before},
        {"full-genome": after},
        (
            ("Fitness", "fitness"),
            ("AttackerWinRate", "attacker_win_rate"),
            ("TurnLimitRate", "turn_limit_rate"),
            ("AverageAttackerTurns", "average_attacker_turn_count"),
            ("MatchFlowScore", "match_flow_score"),
            ("PrimaryRoleIdentityScore", "primary_role_identity_score"),
            ("SecondaryRoleIdentityScore", "secondary_role_identity_score"),
            ("RoleProfileFairnessScore", "role_profile_fairness_score"),
            ("ChangeShapeScore", "change_shape_score"),
        ),
    )


def prepare_eval_content(
    config: config_models.FullGenomeBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    return role_stats.prepare_eval_content_from_config(config, source_content_path)


def build_eval_config(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
) -> eval_api.EvalCommandConfig:
    return role_stats.build_role_alignment_eval_config(config, content_path)


def evaluate_candidate(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    candidate: tuple[int, ...],
    *,
    baseline_unit_templates: list[dict] | None = None,
    grouped_ability_index: grouped_ability_effects.GroupedAbilityIndex | None = None,
) -> FullGenomeMeasurement:
    normalized = normalize_candidate(config, candidate)
    unit_modifiers, ability_multipliers = split_candidate(config, normalized)

    try:
        pct_changes = apply_candidate_to_content(
            config,
            content_path,
            normalized,
            baseline_unit_templates=baseline_unit_templates,
            grouped_ability_index=grouped_ability_index,
        )
        summary = role_stats.run_eval_role_alignment_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            config.ga.evaluation_repeat_stages,
            offensive_ability_ids,
        )
        return build_measurement(
            config,
            summary,
            normalized,
            unit_modifiers,
            ability_multipliers,
            pct_changes,
        )
    except Exception as exc:  # pragma: no cover
        return build_error_measurement(config, normalized, unit_modifiers, ability_multipliers, str(exc))


def evaluate_current_content(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> FullGenomeMeasurement:
    neutral = build_neutral_candidate(config)
    baseline_units = scenario_content.load_unit_templates(content_path)
    grouped_index = grouped_ability_effects.build_grouped_index(content_path)
    return evaluate_candidate(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        neutral,
        baseline_unit_templates=baseline_units,
        grouped_ability_index=grouped_index,
    )


def build_measurement(
    config: config_models.FullGenomeBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
    candidate: FullGenomeCandidate,
    unit_modifiers: UnitProfileModifierMap,
    ability_multipliers: AbilityGroupMultiplierMap,
    pct_changes: list[float],
) -> FullGenomeMeasurement:
    detailed = summary.detailed
    attacker_win_rate = detailed.attacker_wins / detailed.total_runs if detailed.total_runs > 0 else 0.0
    turn_limit_rate = detailed.turn_limit_count / detailed.total_runs if detailed.total_runs > 0 else 1.0

    match_flow_score = compute_match_flow_score(config, attacker_win_rate, turn_limit_rate, detailed.average_attacker_turn_count)
    primary_role_identity_score = compute_primary_role_identity_score(config, summary)
    secondary_role_identity_score = compute_secondary_role_identity_score(config, summary)
    role_profile_fairness_score = compute_role_profile_fairness_score(config, summary)
    change_shape_score = compute_change_shape_score(config, unit_modifiers, pct_changes)

    weights = config.balance.fitness_weights
    fitness = (
        match_flow_score * weights.match_flow
        + primary_role_identity_score * weights.primary_role_identity
        + secondary_role_identity_score * weights.secondary_role_identity
        + role_profile_fairness_score * weights.role_profile_fairness
        + change_shape_score * weights.change_shape
    )

    return FullGenomeMeasurement(
        candidate=candidate,
        unit_profile_modifiers=unit_modifiers,
        ability_group_multipliers=ability_multipliers,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=detailed.average_attacker_turn_count,
        match_flow_score=match_flow_score,
        primary_role_identity_score=primary_role_identity_score,
        secondary_role_identity_score=secondary_role_identity_score,
        role_profile_fairness_score=role_profile_fairness_score,
        change_shape_score=change_shape_score,
        fitness=fitness,
        error_message=None,
    )


def build_error_measurement(
    config: config_models.FullGenomeBalancerConfig,
    candidate: FullGenomeCandidate,
    unit_modifiers: UnitProfileModifierMap,
    ability_multipliers: AbilityGroupMultiplierMap,
    error_message: str,
) -> FullGenomeMeasurement:
    return FullGenomeMeasurement(
        candidate=candidate,
        unit_profile_modifiers=unit_modifiers,
        ability_group_multipliers=ability_multipliers,
        attacker_win_rate=0.0,
        turn_limit_rate=1.0,
        average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
        match_flow_score=-10.0,
        primary_role_identity_score=-10.0,
        secondary_role_identity_score=-10.0,
        role_profile_fairness_score=-10.0,
        change_shape_score=-10.0,
        fitness=-10.0,
        error_message=error_message,
    )


def compute_match_flow_score(
    config: config_models.FullGenomeBalancerConfig,
    attacker_win_rate: float,
    turn_limit_rate: float,
    average_attacker_turn_count: float,
) -> float:
    targets = config.balance.targets.match_flow
    return mean(
        [
            ga.compute_target_band_fitness(attacker_win_rate, *targets.attacker_win_rate),
            ga.compute_target_band_fitness(turn_limit_rate, *targets.turn_limit_rate),
            ga.compute_target_band_fitness(average_attacker_turn_count, *targets.average_attacker_turn_count),
        ]
    )


def compute_primary_role_identity_score(
    config: config_models.FullGenomeBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
) -> float:
    targets = config.balance.targets.primary_role_identity
    units = list(summary.units_by_template_id.values())
    tank_units = [unit for unit in units if unit.primary_role == "Tank"]
    healer_units = [unit for unit in units if unit.primary_role == "Healer"]
    damage_units = [unit for unit in units if unit.primary_role == "Damage"]

    role_shape_score = mean(
        [
            compute_primary_role_score("Tank", summary),
            compute_primary_role_score("Healer", summary),
            compute_primary_role_score("Damage", summary),
        ]
    )
    output_band_score = mean(
        [
            ga.compute_target_band_fitness(safe_mean(tank_units, lambda unit: unit.survival_rate), *targets.tank_survival_rate),
            ga.compute_target_band_fitness(safe_mean(tank_units, lambda unit: unit.average_damage_taken), *targets.tank_average_damage_taken),
            ga.compute_target_band_fitness(safe_mean(tank_units, lambda unit: unit.average_damage_dealt), *targets.tank_average_damage_dealt),
            ga.compute_target_band_fitness(safe_mean(healer_units, lambda unit: unit.average_healing_done), *targets.healer_average_healing_done),
            ga.compute_target_band_fitness(safe_mean(damage_units, lambda unit: unit.average_damage_dealt), *targets.damage_average_damage_dealt),
        ]
    )
    return (role_shape_score * 0.55) + (output_band_score * 0.45)


def compute_secondary_role_identity_score(
    config: config_models.FullGenomeBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
) -> float:
    targets = config.balance.targets.secondary_role_identity
    units = list(summary.units_by_template_id.values())
    buffer_units = [unit for unit in units if unit.secondary_role == "Buffer"]
    debuffer_units = [unit for unit in units if unit.secondary_role == "Debuffer"]
    acrobat_units = [unit for unit in units if unit.secondary_role == "Acrobat"]
    non_acrobat_units = [unit for unit in units if unit.secondary_role != "Acrobat"]

    all_units_tiles_moved = safe_mean(units, lambda unit: unit.average_tiles_moved_total)
    acrobat_tiles_moved = safe_mean(acrobat_units, lambda unit: unit.average_tiles_moved_total)
    non_acrobat_tiles_moved = safe_mean(non_acrobat_units, lambda unit: unit.average_tiles_moved_total)
    acrobat_move_ratio = acrobat_tiles_moved / non_acrobat_tiles_moved if non_acrobat_tiles_moved > 0 else 0.0

    return mean(
        [
            ga.compute_target_band_fitness(
                safe_mean(buffer_units, lambda unit: unit.average_buff_uptime_granted),
                *targets.buffer_average_buff_uptime,
            ),
            ga.compute_target_band_fitness(
                safe_mean(debuffer_units, lambda unit: unit.average_debuff_uptime_granted),
                *targets.debuffer_average_debuff_uptime,
            ),
            ga.compute_target_band_fitness(
                all_units_tiles_moved,
                *targets.all_units_average_tiles_moved_total,
            ),
            ga.compute_target_band_fitness(
                non_acrobat_tiles_moved,
                *targets.non_acrobat_average_tiles_moved_total,
            ),
            ga.compute_target_band_fitness(
                acrobat_tiles_moved,
                *targets.acrobat_average_tiles_moved_total,
            ),
            ga.compute_target_band_fitness(
                acrobat_move_ratio,
                *targets.acrobat_to_non_acrobat_move_ratio,
            ),
        ]
    )


def compute_role_profile_fairness_score(
    config: config_models.FullGenomeBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
) -> float:
    targets = config.balance.targets.role_profile_fairness
    aggregates = [
        aggregate
        for aggregate in summary.role_combination_win_rates.values()
        if aggregate.team_observations > 0
    ]
    if not aggregates:
        return -10.0

    band_score = mean(
        ga.compute_target_band_fitness(aggregate.win_rate, *targets.role_combination_win_rate)
        for aggregate in aggregates
    )
    primary_spread_score = compute_family_spread_score(
        aggregates,
        lambda aggregate: aggregate.primary_role,
        targets.primary_family_win_rate_spread_max,
    )
    secondary_spread_score = compute_family_spread_score(
        aggregates,
        lambda aggregate: aggregate.secondary_role or "None",
        targets.secondary_family_win_rate_spread_max,
    )
    return mean([band_score, primary_spread_score, secondary_spread_score])


def compute_family_spread_score(
    aggregates: list[eval_api.EvalRoleCombinationWinRateAggregate],
    family_key_selector,
    spread_max: float,
) -> float:
    scores: list[float] = []
    family_keys = {family_key_selector(aggregate) for aggregate in aggregates}
    for family_key in family_keys:
        family = [aggregate.win_rate for aggregate in aggregates if family_key_selector(aggregate) == family_key]
        if len(family) < 2:
            continue
        spread = max(family) - min(family)
        scores.append(ga.compute_target_band_fitness(spread, 0.0, spread_max))
    return mean(scores) if scores else 1.0


def compute_change_shape_score(
    config: config_models.FullGenomeBalancerConfig,
    unit_modifiers: UnitProfileModifierMap,
    pct_changes: list[float],
) -> float:
    targets = config.balance.targets.change_shape
    ability_std_dev = statistics.pstdev(pct_changes) if len(pct_changes) >= 2 else 0.0
    unit_profile_spread = statistics.pstdev(build_unit_modifier_fractional_changes(unit_modifiers))
    return mean(
        [
            ga.compute_target_band_fitness(ability_std_dev, *targets.ability_pct_change_std_dev),
            ga.compute_target_band_fitness(unit_profile_spread, *targets.unit_stat_profile_spread_target),
        ]
    )


def build_unit_modifier_fractional_changes(unit_modifiers: UnitProfileModifierMap) -> list[float]:
    changes: list[float] = []
    for hp_multiplier, mana_multiplier, move_delta, physical_dr_delta, magic_dr_delta in unit_modifiers.values():
        changes.extend(
            [
                (hp_multiplier - 100) / 100.0,
                (mana_multiplier - 100) / 100.0,
                move_delta / 10.0,
                physical_dr_delta / 100.0,
                magic_dr_delta / 100.0,
            ]
        )
    return changes


def safe_mean(items: list, selector) -> float:
    if not items:
        return 0.0
    return mean(selector(item) for item in items)


def apply_candidate_to_content(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    candidate: tuple[int, ...],
    *,
    baseline_unit_templates: list[dict] | None = None,
    grouped_ability_index: grouped_ability_effects.GroupedAbilityIndex | None = None,
) -> list[float]:
    normalized = normalize_candidate(config, candidate)
    unit_modifiers, ability_multipliers = split_candidate(config, normalized)
    apply_unit_profile_modifiers_to_content(
        config,
        content_path,
        unit_modifiers,
        baseline_unit_templates=baseline_unit_templates,
    )
    return apply_ability_group_multipliers_to_content(
        config,
        content_path,
        ability_multipliers,
        grouped_ability_index=grouped_ability_index,
    )


def apply_unit_profile_modifiers_to_content(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    unit_modifiers: UnitProfileModifierMap,
    *,
    baseline_unit_templates: list[dict] | None = None,
) -> None:
    source_templates = (
        scenario_content.load_unit_templates(content_path)
        if baseline_unit_templates is None
        else copy_json_objects(baseline_unit_templates)
    )
    templates_by_id = {
        str(unit_template["id"]): unit_template
        for unit_template in source_templates
        if isinstance(unit_template.get("id"), str)
    }

    for profile_name in config.balance.genome.unit_stat_profile_modifiers.profile_order:
        genes = unit_modifiers[profile_name]
        matched_units = [
            unit_template
            for unit_template in source_templates
            if unit_matches_profile(unit_template, profile_name)
        ]
        if not matched_units:
            raise ValueError(f"No unit templates matched full-genome profile {profile_name!r}.")
        for unit_template in matched_units:
            apply_unit_profile_genes(config, unit_template, genes)

    output_templates = list(templates_by_id.values())
    scenario_content.write_json_array(
        content_path / scenario_content.UNIT_TEMPLATES_FILE_NAME,
        output_templates,
    )


def apply_unit_profile_genes(
    config: config_models.FullGenomeBalancerConfig,
    unit_template: dict,
    genes: UnitProfileGenes,
) -> None:
    floors = config.balance.search_space.unit_stat_profile_modifiers.absolute_floors
    ceilings = config.balance.search_space.unit_stat_profile_modifiers.absolute_ceilings

    hp_multiplier, mana_multiplier, move_delta, physical_dr_delta, magic_dr_delta = genes
    unit_template["maxHP"] = max(
        floors.max_hp,
        apply_percent_multiplier(int(unit_template["maxHP"]), hp_multiplier),
    )
    unit_template["maxManaPoints"] = max(
        floors.max_mana_points,
        apply_percent_multiplier(int(unit_template["maxManaPoints"]), mana_multiplier),
    )
    unit_template["movePoints"] = max(
        floors.move_points,
        int(unit_template["movePoints"]) + move_delta,
    )
    unit_template["physicalDamageReceived"] = clamp_int(
        int(unit_template["physicalDamageReceived"]) + physical_dr_delta,
        floors.physical_damage_received_percent,
        ceilings.physical_damage_received_percent,
    )
    unit_template["magicDamageReceived"] = clamp_int(
        int(unit_template["magicDamageReceived"]) + magic_dr_delta,
        floors.magic_damage_received_percent,
        ceilings.magic_damage_received_percent,
    )


def apply_ability_group_multipliers_to_content(
    config: config_models.FullGenomeBalancerConfig,
    content_path: Path,
    ability_multipliers: AbilityGroupMultiplierMap,
    *,
    grouped_ability_index: grouped_ability_effects.GroupedAbilityIndex | None = None,
) -> list[float]:
    index = grouped_ability_index
    if index is None:
        index = grouped_ability_effects.build_grouped_index(content_path)

    ability_config_adapter = build_grouped_ability_config_adapter(config)
    grouped_candidate = build_grouped_ability_candidate(ability_multipliers)
    return grouped_ability_effects.apply_candidate_to_content(
        content_path,
        index,
        ability_config_adapter,
        grouped_candidate,
    )


def build_grouped_ability_candidate(
    ability_multipliers: AbilityGroupMultiplierMap,
) -> grouped_ability_effects.GroupedCandidate:
    return (
        ability_multipliers["tank_damage_percent"],
        ability_multipliers["healer_healing_percent"],
        ability_multipliers["damage_damage_percent"],
        ability_multipliers["buffer_modifier_percent"],
        ability_multipliers["debuffer_modifier_percent"],
        ability_multipliers["mana_cost_percent"],
    )


def build_grouped_ability_config_adapter(
    config: config_models.FullGenomeBalancerConfig,
) -> SimpleNamespace:
    floors = config.balance.search_space.ability_effect_groups.floors
    return SimpleNamespace(
        balance=SimpleNamespace(
            damage_floor=floors.damage,
            heal_floor=floors.heal,
            percent_mod_floor=floors.percent_modifier_abs,
            flat_mod_floor=floors.flat_modifier_abs,
            mana_cost_floor=floors.mana_cost,
        )
    )


def unit_matches_profile(unit_template: dict, profile_name: str) -> bool:
    primary_role, secondary_role = split_profile_name(profile_name)
    return (
        unit_template.get("primaryRole") == primary_role
        and unit_template.get("secondaryRole") == secondary_role
    )


def split_profile_name(profile_name: str) -> tuple[str, str | None]:
    if "+" not in profile_name:
        return profile_name, None
    primary_role, secondary_role = profile_name.split("+", 1)
    return primary_role, secondary_role


def apply_percent_multiplier(value: int, multiplier_percent: int) -> int:
    return round(value * multiplier_percent / 100.0)


def clamp_int(value: int, minimum: int, maximum: int) -> int:
    return max(minimum, min(maximum, int(value)))


def copy_json_objects(items: list[dict]) -> list[dict]:
    return [dict(item) for item in items]
