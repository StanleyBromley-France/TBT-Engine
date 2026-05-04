#!/usr/bin/env python3
"""Tune ability effects with grouped role/category multipliers."""
from __future__ import annotations

import argparse
import math
import random
from dataclasses import dataclass
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.measurement_models as measurements
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.scenarios import content as scenario_content
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.workflows import ability_effects
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow


GroupedCandidate = tuple[int, int, int, int, int, int]
GROUP_NAMES = (
    "tank-damage",
    "healer-healing",
    "damage-damage",
    "buffer-modifier",
    "debuffer-modifier",
    "mana-cost",
)


@dataclass(frozen=True)
class AbilityOwner:
    primary_role: str
    secondary_role: str | None


@dataclass(frozen=True)
class ComponentOwnerContext:
    owners: tuple[AbilityOwner, ...]
    harmful: bool


@dataclass(frozen=True)
class GroupedAbilityEffectsMeasurement:
    tank_damage_percent: int
    healer_healing_percent: int
    damage_damage_percent: int
    buffer_modifier_percent: int
    debuffer_modifier_percent: int
    mana_cost_percent: int
    attacker_win_rate: float
    average_tank_damage_dealt: float
    average_healer_healing_done: float
    average_damage_damage_dealt: float
    average_buffer_buff_uptime: float
    average_debuffer_debuff_uptime: float
    pct_change_std_dev: float
    win_rate_score: float
    primary_role_score: float
    role_tradeoff_score: float
    role_dominance_score: float
    secondary_role_score: float
    role_combination_win_rate_score: float
    diversity_score: float
    fitness: float
    error_message: str | None


@dataclass(frozen=True)
class GroupedAbilityIndex:
    components: list[dict]
    abilities: list[dict]
    component_context_by_id: dict[str, ComponentOwnerContext]
    component_baselines: dict[str, dict[str, int]]
    mana_cost_baselines: dict[str, int]


def load_balancer_config(args: argparse.Namespace) -> config_models.AbilityEffectsBalancerConfig:
    return load_balancer_config_from_args(
        config_models.AbilityEffectsBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def validate_config(config: config_models.AbilityEffectsBalancerConfig) -> None:
    ability_effects.validate_config(config)


def prepare_eval_content(
    config: config_models.AbilityEffectsBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    return ability_effects.prepare_eval_content(config, source_content_path)


def build_eval_config(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
) -> eval_api.EvalCommandConfig:
    return ability_effects.build_eval_config(config, content_path)


def percent_bounds(max_pct_change: float) -> tuple[int, int]:
    return (round(100 * (1.0 - max_pct_change)), round(100 * (1.0 + max_pct_change)))


def normalize_candidate(
    candidate: tuple[int, ...],
    config: config_models.AbilityEffectsBalancerConfig,
) -> GroupedCandidate:
    component_low, component_high = percent_bounds(config.balance.component_max_pct_change)
    modifier_low, modifier_high = percent_bounds(config.balance.percent_mod_max_pct_change)
    mana_low, mana_high = percent_bounds(config.balance.mana_cost_max_pct_change)
    values = (
        clamp_int(candidate[0], component_low, component_high),
        clamp_int(candidate[1], component_low, component_high),
        clamp_int(candidate[2], component_low, component_high),
        clamp_int(candidate[3], modifier_low, modifier_high),
        clamp_int(candidate[4], modifier_low, modifier_high),
        clamp_int(candidate[5], mana_low, mana_high),
    )
    return values


def clamp_int(value: int, low: int, high: int) -> int:
    return max(low, min(high, int(value)))


def build_grouped_index(content_path: Path) -> GroupedAbilityIndex:
    components = scenarios.load_effect_components(content_path)
    abilities = scenarios.load_abilities(content_path)
    effects = scenario_content.load_json_array(content_path / "effectTemplates.json")
    units = scenarios.load_unit_templates(content_path)

    ability_owners = build_ability_owners(units)
    effect_by_id = {effect["id"]: effect for effect in effects if isinstance(effect.get("id"), str)}
    ability_by_effect_id: dict[str, list[dict]] = {}
    for ability in abilities:
        effect_id = ability.get("effectTemplateId")
        if isinstance(effect_id, str):
            ability_by_effect_id.setdefault(effect_id, []).append(ability)

    component_context_by_id: dict[str, ComponentOwnerContext] = {}
    for effect_id, effect in effect_by_id.items():
        component_ids = effect.get("componentTemplateIds", [])
        if not isinstance(component_ids, list):
            continue
        owners = tuple(
            owner
            for ability in ability_by_effect_id.get(effect_id, [])
            for owner in ability_owners.get(str(ability.get("id")), ())
        )
        harmful = bool(effect.get("isHarmful", False))
        for component_id in component_ids:
            if isinstance(component_id, str):
                existing = component_context_by_id.get(component_id)
                if existing is None:
                    component_context_by_id[component_id] = ComponentOwnerContext(owners, harmful)
                else:
                    component_context_by_id[component_id] = ComponentOwnerContext(
                        tuple({*existing.owners, *owners}),
                        existing.harmful or harmful,
                    )

    return GroupedAbilityIndex(
        components=components,
        abilities=abilities,
        component_context_by_id=component_context_by_id,
        component_baselines=build_component_baselines(components),
        mana_cost_baselines={
            ability["id"]: int(ability.get("manaCost", 0))
            for ability in abilities
            if isinstance(ability.get("id"), str) and int(ability.get("manaCost", 0)) > 0
        },
    )


def build_ability_owners(units: list[dict]) -> dict[str, tuple[AbilityOwner, ...]]:
    owners_by_ability: dict[str, list[AbilityOwner]] = {}
    for unit in units:
        primary_role = unit.get("primaryRole")
        if not isinstance(primary_role, str):
            continue
        secondary_role = unit.get("secondaryRole")
        if not isinstance(secondary_role, str):
            secondary_role = None
        owner = AbilityOwner(primary_role, secondary_role)
        ability_ids = unit.get("abilityIds", [])
        if not isinstance(ability_ids, list):
            continue
        for ability_id in ability_ids:
            if isinstance(ability_id, str):
                owners_by_ability.setdefault(ability_id, []).append(owner)
    return {key: tuple(value) for key, value in owners_by_ability.items()}


def build_component_baselines(components: list[dict]) -> dict[str, dict[str, int]]:
    baselines: dict[str, dict[str, int]] = {}
    for component in components:
        component_id = component.get("id")
        if not isinstance(component_id, str):
            continue
        fields: dict[str, int] = {}
        for field_name in ("damage", "heal", "percent", "amount"):
            if field_name in component:
                fields[field_name] = int(component[field_name])
        baselines[component_id] = fields
    return baselines


def apply_candidate_to_content(
    content_path: Path,
    index: GroupedAbilityIndex,
    config: config_models.AbilityEffectsBalancerConfig,
    candidate: GroupedCandidate,
) -> list[float]:
    component_updates: dict[str, dict[str, int]] = {}
    pct_changes: list[float] = []
    for component in index.components:
        component_id = component.get("id")
        if not isinstance(component_id, str):
            continue
        baseline_fields = index.component_baselines.get(component_id, {})
        context = index.component_context_by_id.get(component_id, ComponentOwnerContext((), False))
        updates = build_component_updates(component, baseline_fields, context, config, candidate, pct_changes)
        if updates:
            component_updates[component_id] = updates

    mana_updates: dict[str, int] = {}
    mana_multiplier = candidate[5]
    for ability_id, baseline in index.mana_cost_baselines.items():
        updated = apply_multiplier(baseline, mana_multiplier, config.balance.mana_cost_floor)
        mana_updates[ability_id] = updated
        pct_changes.append(fractional_change(baseline, updated))

    scenarios.update_effect_component_values(content_path, component_updates)
    scenarios.update_ability_mana_costs(content_path, mana_updates)
    return pct_changes


def build_component_updates(
    component: dict,
    baseline_fields: dict[str, int],
    context: ComponentOwnerContext,
    config: config_models.AbilityEffectsBalancerConfig,
    candidate: GroupedCandidate,
    pct_changes: list[float],
) -> dict[str, int]:
    component_type = component.get("type", "")
    updates: dict[str, int] = {}
    if component_type in ("InstantDamage", "DamageOverTime") and "damage" in baseline_fields:
        multiplier = primary_damage_multiplier(context.owners, candidate)
        updated = apply_multiplier(baseline_fields["damage"], multiplier, config.balance.damage_floor)
        updates["damage"] = updated
        pct_changes.append(fractional_change(baseline_fields["damage"], updated))
    elif component_type in ("InstantHeal", "HealOverTime") and "heal" in baseline_fields:
        multiplier = healer_multiplier(context.owners, candidate)
        updated = apply_multiplier(baseline_fields["heal"], multiplier, config.balance.heal_floor)
        updates["heal"] = updated
        pct_changes.append(fractional_change(baseline_fields["heal"], updated))
    elif component_type == "PercentAttributeModifier" and "percent" in baseline_fields:
        multiplier = support_modifier_multiplier(context, candidate)
        updated = apply_signed_multiplier(baseline_fields["percent"], multiplier, config.balance.percent_mod_floor)
        updates["percent"] = updated
        pct_changes.append(fractional_change(baseline_fields["percent"], updated))
    elif component_type == "FlatAttributeModifier" and "amount" in baseline_fields:
        stat = component.get("stat", "")
        if stat in ("MaxHP", "MovePoints"):
            multiplier = support_modifier_multiplier(context, candidate)
            updated = apply_signed_multiplier(baseline_fields["amount"], multiplier, config.balance.flat_mod_floor)
            updates["amount"] = updated
            pct_changes.append(fractional_change(baseline_fields["amount"], updated))
    return updates


def primary_damage_multiplier(owners: tuple[AbilityOwner, ...], candidate: GroupedCandidate) -> int:
    multipliers: list[int] = []
    for owner in owners:
        if owner.primary_role == "Tank":
            multipliers.append(candidate[0])
        elif owner.primary_role == "Damage":
            multipliers.append(candidate[2])
    return round(sum(multipliers) / len(multipliers)) if multipliers else 100


def healer_multiplier(owners: tuple[AbilityOwner, ...], candidate: GroupedCandidate) -> int:
    return candidate[1] if any(owner.primary_role == "Healer" for owner in owners) else 100


def support_modifier_multiplier(context: ComponentOwnerContext, candidate: GroupedCandidate) -> int:
    multipliers: list[int] = []
    if any(owner.secondary_role == "Buffer" for owner in context.owners) and not context.harmful:
        multipliers.append(candidate[3])
    if any(owner.secondary_role == "Debuffer" for owner in context.owners) or context.harmful:
        multipliers.append(candidate[4])
    return round(sum(multipliers) / len(multipliers)) if multipliers else 100


def apply_multiplier(value: int, multiplier_percent: int, floor: int) -> int:
    return max(floor, round(value * multiplier_percent / 100.0))


def apply_signed_multiplier(value: int, multiplier_percent: int, floor: int) -> int:
    if value == 0:
        return 0
    sign = 1 if value > 0 else -1
    magnitude = max(floor, round(abs(value) * multiplier_percent / 100.0))
    return sign * magnitude


def fractional_change(before: int, after: int) -> float:
    if before == 0:
        return 0.0
    return (after - before) / before


def pct_change_std_dev(pct_changes: list[float]) -> float:
    if not pct_changes:
        return 0.0
    mean = sum(pct_changes) / len(pct_changes)
    return math.sqrt(sum((value - mean) ** 2 for value in pct_changes) / len(pct_changes))


def evaluate_candidate(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    index: GroupedAbilityIndex,
    candidate: GroupedCandidate,
) -> GroupedAbilityEffectsMeasurement:
    normalized = normalize_candidate(candidate, config)
    pct_changes = apply_candidate_to_content(content_path, index, config, normalized)
    try:
        summary = eval_api.run_staged_total_repeat_schedule(
            config.ga.evaluation_repeat_stages,
            lambda repeat_count: eval_api.run_eval_role_alignment(
                eval_config.with_repeat_count(repeat_count),
                config.ga.evaluation_turn_budget,
                offensive_ability_ids,
            ),
        )
        measurement = ability_effects.compute_ability_effects_score(
            config.balance,
            summary,
            pct_changes,
        )
    except Exception as exc:  # pragma: no cover
        measurement = measurements.AbilityEffectsMeasurement(
            attacker_win_rate=0.0,
            average_tank_damage_dealt=0.0,
            average_healer_healing_done=0.0,
            average_damage_damage_dealt=0.0,
            average_buffer_buff_uptime=0.0,
            average_debuffer_debuff_uptime=0.0,
            pct_change_std_dev=pct_change_std_dev(pct_changes),
            win_rate_score=-10.0,
            primary_role_score=-10.0,
            role_tradeoff_score=-10.0,
            role_dominance_score=-10.0,
            secondary_role_score=-10.0,
            role_combination_win_rate_score=-10.0,
            diversity_score=-10.0,
            fitness=-10.0,
            error_message=str(exc),
        )
    return build_grouped_measurement(normalized, measurement)


def build_grouped_measurement(
    candidate: GroupedCandidate,
    measurement: measurements.AbilityEffectsMeasurement,
) -> GroupedAbilityEffectsMeasurement:
    return GroupedAbilityEffectsMeasurement(
        *candidate,
        attacker_win_rate=measurement.attacker_win_rate,
        average_tank_damage_dealt=measurement.average_tank_damage_dealt,
        average_healer_healing_done=measurement.average_healer_healing_done,
        average_damage_damage_dealt=measurement.average_damage_damage_dealt,
        average_buffer_buff_uptime=measurement.average_buffer_buff_uptime,
        average_debuffer_debuff_uptime=measurement.average_debuffer_debuff_uptime,
        pct_change_std_dev=measurement.pct_change_std_dev,
        win_rate_score=measurement.win_rate_score,
        primary_role_score=measurement.primary_role_score,
        role_tradeoff_score=measurement.role_tradeoff_score,
        role_dominance_score=measurement.role_dominance_score,
        secondary_role_score=measurement.secondary_role_score,
        role_combination_win_rate_score=measurement.role_combination_win_rate_score,
        diversity_score=measurement.diversity_score,
        fitness=measurement.fitness,
        error_message=measurement.error_message,
    )


class GroupedAbilityEffectsWorkflow(CandidateWorkflow[GroupedCandidate, GroupedAbilityEffectsMeasurement]):
    def __init__(
        self,
        config: config_models.AbilityEffectsBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
        index: GroupedAbilityIndex,
    ):
        self.creator_name_prefix = "GroupedAbilityEffects"
        self.random_seed = config.ga.ga_random_seed
        self.population_size = config.ga.candidate_population_size
        self.generation_count = config.ga.generation_count
        self.mutation_probability = config.ga.mutation_probability
        self.crossover_probability = config.ga.crossover_probability
        self.config = config
        self.content_path = content_path
        self.eval_config = eval_config
        self.offensive_ability_ids = offensive_ability_ids
        self.index = index
        self.initial_candidate: GroupedCandidate = (100, 100, 100, 100, 100, 100)

    def normalize_individual(self, individual: list[int]) -> GroupedCandidate:
        return normalize_candidate(tuple(int(value) for value in individual), self.config)

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        population: list = [individual_type(list(self.initial_candidate))]
        component_low, component_high = percent_bounds(self.config.balance.component_max_pct_change)
        modifier_low, modifier_high = percent_bounds(self.config.balance.percent_mod_max_pct_change)
        mana_low, mana_high = percent_bounds(self.config.balance.mana_cost_max_pct_change)
        while len(population) < self.population_size:
            population.append(
                individual_type(
                    list(
                        self.normalize_individual(
                            [
                                rng.randint(component_low, component_high),
                                rng.randint(component_low, component_high),
                                rng.randint(component_low, component_high),
                                rng.randint(modifier_low, modifier_high),
                                rng.randint(modifier_low, modifier_high),
                                rng.randint(mana_low, mana_high),
                            ]
                        )
                    )
                )
            )
        return population[: self.population_size]

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        bounds = [
            percent_bounds(self.config.balance.component_max_pct_change),
            percent_bounds(self.config.balance.component_max_pct_change),
            percent_bounds(self.config.balance.component_max_pct_change),
            percent_bounds(self.config.balance.percent_mod_max_pct_change),
            percent_bounds(self.config.balance.percent_mod_max_pct_change),
            percent_bounds(self.config.balance.mana_cost_max_pct_change),
        ]
        for index, (lo, hi) in enumerate(bounds):
            if rng.random() < 0.6:
                step = max(1, (hi - lo) // 8)
                individual[index] = clamp_int(individual[index] + rng.randint(-step, step), lo, hi)
            elif rng.random() < 0.3:
                individual[index] = rng.randint(lo, hi)
        return (individual,)

    def evaluate_candidate(self, candidate: GroupedCandidate) -> GroupedAbilityEffectsMeasurement:
        return evaluate_candidate(
            self.config,
            self.content_path,
            self.eval_config,
            self.offensive_ability_ids,
            self.index,
            candidate,
        )

    def get_fitness(self, measurement: GroupedAbilityEffectsMeasurement) -> float:
        return measurement.fitness

    def on_candidate(
        self,
        measurement: GroupedAbilityEffectsMeasurement,
        elapsed_seconds: float,
        cached: bool,
    ) -> None:
        print_grouped_candidate("candidate", measurement, elapsed_seconds=elapsed_seconds, cached=cached, detailed=False)

    def on_generation_best(self, generation: int, measurement: GroupedAbilityEffectsMeasurement) -> None:
        print_grouped_candidate(f"generation {generation} best", measurement, banner=True)


def optimize_grouped_ability_effects(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    index: GroupedAbilityIndex,
) -> GroupedAbilityEffectsMeasurement:
    workflow = GroupedAbilityEffectsWorkflow(config, content_path, eval_config, offensive_ability_ids, index)
    best_key, best_measurement = run_candidate_workflow(workflow)
    apply_candidate_to_content(content_path, index, config, best_key)
    return best_measurement


def evaluate_initial_grouped_ability_effects(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    index: GroupedAbilityIndex,
) -> GroupedAbilityEffectsMeasurement:
    return evaluate_candidate(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        index,
        (100, 100, 100, 100, 100, 100),
    )


def print_grouped_candidate(
    prefix: str,
    measurement: GroupedAbilityEffectsMeasurement,
    *,
    elapsed_seconds: float | None = None,
    cached: bool | None = None,
    banner: bool = False,
    detailed: bool = True,
) -> None:
    fields = []
    if elapsed_seconds is not None:
        fields.append(reporting.field("elapsed", f"{elapsed_seconds:.1f}s"))
    if cached is not None:
        fields.append(reporting.field("cached", str(cached).lower()))
    fields.extend(summary_fields(measurement, detailed=detailed))
    if banner:
        reporting.print_section(prefix, fields)
    else:
        reporting.print_record(prefix, fields)


def summary_fields(measurement: GroupedAbilityEffectsMeasurement, *, detailed: bool) -> list[reporting.Field]:
    fields = [
        reporting.field("fitness", measurement.fitness, ".4f"),
        reporting.field("winrate", measurement.attacker_win_rate, ".2%"),
    ]
    if detailed:
        fields.extend([
        reporting.field("tank-dmg", measurement.average_tank_damage_dealt, ".1f"),
        reporting.field("healer-heal", measurement.average_healer_healing_done, ".1f"),
        reporting.field("damage-dmg", measurement.average_damage_damage_dealt, ".1f"),
        reporting.field("buffer-uptime", measurement.average_buffer_buff_uptime, ".2f"),
        reporting.field("debuffer-uptime", measurement.average_debuffer_debuff_uptime, ".2f"),
        reporting.field("diversity", measurement.pct_change_std_dev, ".4f"),
        ])
    return fields


def measurement_as_candidate(measurement: GroupedAbilityEffectsMeasurement) -> GroupedCandidate:
    return (
        measurement.tank_damage_percent,
        measurement.healer_healing_percent,
        measurement.damage_damage_percent,
        measurement.buffer_modifier_percent,
        measurement.debuffer_modifier_percent,
        measurement.mana_cost_percent,
    )


def build_package_report(
    before: GroupedAbilityEffectsMeasurement,
    after: GroupedAbilityEffectsMeasurement,
) -> dict:
    return reporting.build_evidence_report(
        {"ability-effects": before},
        {"ability-effects": after},
        (
            ("Fitness", "fitness"),
            ("AttackerWinRate", "attacker_win_rate"),
            ("TankDamagePercent", "tank_damage_percent"),
            ("HealerHealingPercent", "healer_healing_percent"),
            ("DamageDamagePercent", "damage_damage_percent"),
            ("BufferModifierPercent", "buffer_modifier_percent"),
            ("DebufferModifierPercent", "debuffer_modifier_percent"),
            ("ManaCostPercent", "mana_cost_percent"),
            ("PrimaryRoleScore", "primary_role_score"),
            ("SecondaryRoleScore", "secondary_role_score"),
            ("DiversityScore", "diversity_score"),
        ),
    )


def run(
    config: config_models.AbilityEffectsBalancerConfig,
    source_content_path: Path | None = None,
    output_package_path: Path | None = None,
    persist_results: bool = True,
) -> int:
    runtime.ensure_deap_available()
    validate_config(config)
    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path

    content_path = prepare_eval_content(config, content_source)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    index = build_grouped_index(content_path)

    reporting.print_record(
        "optimising ability effects",
        [
            reporting.field("groups", len(GROUP_NAMES)),
            reporting.field("population", config.ga.candidate_population_size),
            reporting.field("generations", config.ga.generation_count),
        ],
    )

    before = evaluate_initial_grouped_ability_effects(config, content_path, eval_config, offensive_ability_ids, index)
    best = optimize_grouped_ability_effects(config, content_path, eval_config, offensive_ability_ids, index)
    print_grouped_candidate("best", best)

    if output_package_path is not None:
        balance_package.write_balance_package(
            output_package_path,
            "ability-effects",
            content_source,
            content_path,
            build_package_report(before, best),
            changed_files=("effectComponentTemplates.json", "abilities.json"),
        )
        print(f"package={output_package_path}", flush=True)

    if persist_results:
        scenarios.save_file_to_source_content(content_path, content_source, "effectComponentTemplates.json")
        scenarios.save_file_to_source_content(content_path, content_source, "abilities.json")
        print(f"saved effectComponentTemplates.json + abilities.json to {content_source}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0
