#!/usr/bin/env python3
"""Tune ability effect component values and mana costs for all units.

Reads effectComponentTemplates.json and abilities.json, builds a unified
parameter vector covering every tunable numeric field:

  Component fields (damage, heal, percent, flat amount)
    → control how hard abilities hit / how much they heal / buff strength
  Ability manaCost fields (non-zero costs only; zero = basic attack, stays free)
    → control how often abilities are used per game

The GA searches for the combination that best satisfies per-role targets:

  - Tank primary: moderate damage output (tanks hit but don't dominate)
  - Healer primary: meaningful healing throughput
  - Damage primary: high damage output
  - Buffer secondary: sustained buff uptime across allies
  - Debuffer secondary: sustained debuff uptime on enemies

Mana costs are a natural lever for controlling total output without touching
per-hit values: raising a cost reduces cast frequency while keeping each cast
at full strength. The optimizer will use whichever combination of cost and
power best satisfies all targets simultaneously.
"""
from __future__ import annotations

import argparse
import random
from dataclasses import dataclass
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.measurement_models as measurements
import auto_balancer.package as balance_package
import auto_balancer.reporting as reporting
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow
from auto_balancer.config import load_balancer_config_from_args
from balancing_scripts.ability_effects.scoring import compute_ability_effects_score


# ── Parameter index ───────────────────────────────────────────────────────────

@dataclass(frozen=True)
class ComponentParameter:
    """One tunable numeric field in an effect component template."""
    component_id: str
    field_name: str
    min_val: int
    max_val: int
    initial_value: int


def _pct_bounds(initial: int, max_pct: float, floor: int) -> tuple[int, int]:
    """Return a ±max_pct window around the authored baseline, floored at `floor`.

    The authored value is treated as a well-balanced starting point; the GA
    expresses corrections as a percentage of that baseline rather than searching
    an absolute range.  The floor prevents values reaching zero or flipping sign.

    For negative authored values (debuff magnitudes stored as signed ints) the
    window is symmetric in absolute-value space: the sign is preserved and the
    magnitude is bounded by [abs(initial) * (1-max_pct), abs(initial) * (1+max_pct)].
    """
    if initial == 0:
        return floor, floor  # degenerate: leave as-is

    if initial > 0:
        lo = max(floor, round(initial * (1.0 - max_pct)))
        hi = round(initial * (1.0 + max_pct))
    else:
        # Negative value — work in magnitude space, re-apply sign
        mag = abs(initial)
        lo_mag = max(floor, round(mag * (1.0 - max_pct)))
        hi_mag = round(mag * (1.0 + max_pct))
        # Return as negative: lo is most-negative (hi_mag), hi is least-negative (lo_mag)
        lo, hi = -hi_mag, -lo_mag

    return lo, hi


def build_component_parameter_index(
    components: list[dict],
    config: config_models.AbilityEffectsBalanceConfig,
) -> list[ComponentParameter]:
    """Derive the ordered list of tunable parameters from the component file.

    Each component type contributes one entry:
      InstantDamage / DamageOverTime → damage field
      InstantHeal / HealOverTime     → heal field
      PercentAttributeModifier       → percent field (sign-preserving bounds)
      FlatAttributeModifier          → amount field

    Bounds are [initial × (1 ± max_pct)] clamped to the appropriate safety
    floor. The authored values are the balanced baseline; the GA applies
    percentage corrections rather than searching an absolute range.
    """
    params: list[ComponentParameter] = []
    for component in components:
        comp_type = component.get("type", "")
        comp_id = component.get("id", "")
        if not comp_id:
            continue

        if comp_type in ("InstantDamage", "DamageOverTime"):
            initial = int(component.get("damage", 14))
            lo, hi = _pct_bounds(initial, config.component_max_pct_change, config.damage_floor)
            params.append(ComponentParameter(comp_id, "damage", lo, hi, initial))

        elif comp_type in ("InstantHeal", "HealOverTime"):
            initial = int(component.get("heal", 16))
            lo, hi = _pct_bounds(initial, config.component_max_pct_change, config.heal_floor)
            params.append(ComponentParameter(comp_id, "heal", lo, hi, initial))

        elif comp_type == "PercentAttributeModifier":
            initial = int(component.get("percent", 15))
            lo, hi = _pct_bounds(initial, config.percent_mod_max_pct_change, config.percent_mod_floor)
            params.append(ComponentParameter(comp_id, "percent", lo, hi, initial))

        elif comp_type == "FlatAttributeModifier":
            stat = component.get("stat", "")
            if stat not in ("MaxHP", "MovePoints"):
                continue
            initial = int(component.get("amount", 1))
            lo, hi = _pct_bounds(initial, config.flat_mod_max_pct_change, config.flat_mod_floor)
            params.append(ComponentParameter(comp_id, "amount", lo, hi, initial))

    return params


def build_mana_cost_parameter_index(
    abilities: list[dict],
    config: config_models.AbilityEffectsBalanceConfig,
) -> list[ComponentParameter]:
    """Derive tunable mana cost parameters from abilities.json.

    Only abilities with manaCost > 0 are included — zero-cost abilities are
    basic-attack fallbacks and must stay free.

    Bounds are [initial × (1 ± mana_cost_max_pct_change)] clamped to
    mana_cost_floor, preserving relative cost ordering between abilities.
    """
    params: list[ComponentParameter] = []
    for ability in abilities:
        ability_id = ability.get("id", "")
        current_cost = int(ability.get("manaCost", 0))
        if not ability_id or current_cost == 0:
            continue
        lo, hi = _pct_bounds(current_cost, config.mana_cost_max_pct_change, config.mana_cost_floor)
        params.append(ComponentParameter(ability_id, "manaCost", lo, hi, current_cost))
    return params


def extract_initial_candidate(
    parameter_index: list[ComponentParameter],
) -> tuple[int, ...]:
    return tuple(param.initial_value for param in parameter_index)


def normalize_candidate(
    candidate: tuple[int, ...],
    parameter_index: list[ComponentParameter],
) -> tuple[int, ...]:
    return tuple(
        ga.bounded_integer(value, param.min_val, param.max_val)
        for value, param in zip(candidate, parameter_index)
    )


def candidate_to_updates(
    candidate: tuple[int, ...],
    parameter_index: list[ComponentParameter],
) -> dict[str, dict[str, int]]:
    """Convert a flat candidate vector to {id: {field: value}} updates."""
    updates: dict[str, dict[str, int]] = {}
    for param, value in zip(parameter_index, candidate):
        updates.setdefault(param.component_id, {})[param.field_name] = value
    return updates


# ── Config loading ────────────────────────────────────────────────────────────

def load_balancer_config(args: argparse.Namespace) -> config_models.AbilityEffectsBalancerConfig:
    return load_balancer_config_from_args(
        config_models.AbilityEffectsBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


# ── Content and eval helpers ──────────────────────────────────────────────────

def prepare_eval_content(
    config: config_models.AbilityEffectsBalancerConfig,
    source_content_path: Path | None = None,
) -> Path:
    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path
    scenario_config = scenarios.ScenarioGenerationConfig(
        seed=config.scenario.scenario_generation_random_seed,
        generated_scenarios_per_run=config.scenario.generated_scenario_count,
        map_width=config.scenario.map_width_tiles,
        map_height=config.scenario.map_height_tiles,
    )
    generated_content_path = scenarios.build_generated_content_path(
        content_source,
        config.scenario.scenario_generation_random_seed,
        config.scenario.generated_scenario_count,
    )
    return scenarios.prepare_generated_content(
        source_content_path=content_source,
        generated_content_path=generated_content_path,
        config=scenario_config,
    )


def build_eval_config(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
) -> eval_api.EvalCommandConfig:
    final_repeat_count = config.ga.evaluation_repeat_stages[-1].total_repeats
    return eval_api.create_eval_config(
        cli_path=None,
        content_path=content_path,
        game_state=config.scenario.game_state_id,
        validation=config.scenario.validation_mode,
        seed=config.scenario.evaluation_random_seed,
        repeat_count=final_repeat_count,
        timeout_seconds=config.ga.evaluation_timeout_seconds,
        log_mode=config.ga.evaluation_log_mode,
    )


def apply_candidate_to_content(
    content_path: Path,
    candidate: tuple[int, ...],
    component_index: list[ComponentParameter],
    mana_cost_index: list[ComponentParameter],
) -> None:
    n_components = len(component_index)
    component_candidate = candidate[:n_components]
    mana_candidate = candidate[n_components:]

    component_updates = candidate_to_updates(component_candidate, component_index)
    scenarios.update_effect_component_values(content_path, component_updates)

    mana_updates_nested = candidate_to_updates(mana_candidate, mana_cost_index)
    # mana_updates_nested is {ability_id: {"manaCost": value}} — flatten to {ability_id: cost}
    mana_updates = {ability_id: fields["manaCost"] for ability_id, fields in mana_updates_nested.items()}
    scenarios.update_ability_mana_costs(content_path, mana_updates)


# ── Evaluation ────────────────────────────────────────────────────────────────

def evaluate_candidate(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    component_index: list[ComponentParameter],
    mana_cost_index: list[ComponentParameter],
    candidate: tuple[int, ...],
    repeat_stages: tuple[eval_api.RepeatStage, ...] | None = None,
) -> measurements.AbilityEffectsMeasurement:
    full_index = component_index + mana_cost_index
    normalized = normalize_candidate(candidate, full_index)
    apply_candidate_to_content(content_path, normalized, component_index, mana_cost_index)
    staged_repeats = config.ga.evaluation_repeat_stages if repeat_stages is None else repeat_stages

    try:
        summary = eval_api.run_staged_total_repeat_schedule(
            staged_repeats,
            lambda repeat_count: eval_api.run_eval_role_alignment(
                eval_config.with_repeat_count(repeat_count),
                config.ga.evaluation_turn_budget,
                offensive_ability_ids,
            ),
        )
    except Exception as exc:  # pragma: no cover
        return measurements.AbilityEffectsMeasurement(
            attacker_win_rate=0.0,
            average_tank_damage_dealt=0.0,
            average_healer_healing_done=0.0,
            average_damage_damage_dealt=0.0,
            average_buffer_buff_uptime=0.0,
            average_debuffer_debuff_uptime=0.0,
            pct_change_std_dev=0.0,
            win_rate_score=-10.0,
            primary_role_score=-10.0,
            role_tradeoff_score=-10.0,
            role_dominance_score=-10.0,
            secondary_role_score=-10.0,
            diversity_score=-10.0,
            fitness=-10.0,
            error_message=str(exc),
        )

    # Compute fractional changes for the diversity scorer.
    # Each element is (final − initial) / initial for every tuned parameter.
    # Parameters whose initial value is 0 are skipped (division by zero guard),
    # but in practice all parameters have non-zero authored values.
    pct_changes: list[float] = [
        (final - param.initial_value) / param.initial_value
        for final, param in zip(normalized, full_index)
        if param.initial_value != 0
    ]

    return compute_ability_effects_score(config.balance, summary, pct_changes)


# ── GA ────────────────────────────────────────────────────────────────────────

def build_initial_population(
    config: config_models.AbilityEffectsBalancerConfig,
    parameter_index: list[ComponentParameter],
    initial_candidate: tuple[int, ...],
    rng: random.Random,
    individual_type: type,
) -> list:
    normalized_seed = normalize_candidate(initial_candidate, parameter_index)
    population: list = [individual_type(list(normalized_seed))]
    seen: set[tuple[int, ...]] = {normalized_seed}

    while len(population) < config.ga.candidate_population_size:
        raw = tuple(
            rng.randint(param.min_val, param.max_val)
            for param in parameter_index
        )
        normalized = normalize_candidate(raw, parameter_index)
        if normalized not in seen:
            seen.add(normalized)
            population.append(individual_type(list(normalized)))

    return population[: config.ga.candidate_population_size]


def mutate_candidate(
    individual: list[int],
    parameter_index: list[ComponentParameter],
    rng: random.Random,
) -> tuple[list[int]]:
    for index, param in enumerate(parameter_index):
        roll = rng.random()
        if roll < 0.40:
            span = max(1, (param.max_val - param.min_val) // 6)
            individual[index] = ga.bounded_integer(
                individual[index] + rng.randint(-span, span),
                param.min_val,
                param.max_val,
            )
        elif roll < 0.55:
            individual[index] = rng.randint(param.min_val, param.max_val)
    return (individual,)


class AbilityEffectsWorkflow(CandidateWorkflow[tuple[int, ...], measurements.AbilityEffectsMeasurement]):
    def __init__(
        self,
        config: config_models.AbilityEffectsBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
        component_index: list[ComponentParameter],
        mana_cost_index: list[ComponentParameter],
        initial_candidate: tuple[int, ...],
    ):
        self.creator_name_prefix = "AbilityEffects"
        self.random_seed = config.ga.ga_random_seed
        self.population_size = config.ga.candidate_population_size
        self.generation_count = config.ga.generation_count
        self.mutation_probability = config.ga.mutation_probability
        self.config = config
        self.content_path = content_path
        self.eval_config = eval_config
        self.offensive_ability_ids = offensive_ability_ids
        self.component_index = component_index
        self.mana_cost_index = mana_cost_index
        self.parameter_index = component_index + mana_cost_index
        self.initial_candidate = initial_candidate

    def normalize_individual(self, individual: list[int]) -> tuple[int, ...]:
        return normalize_candidate(tuple(int(v) for v in individual), self.parameter_index)

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        return build_initial_population(
            self.config,
            self.parameter_index,
            self.initial_candidate,
            rng,
            individual_type,
        )

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        return mutate_candidate(individual, self.parameter_index, rng)

    def evaluate_candidate(self, candidate: tuple[int, ...]) -> measurements.AbilityEffectsMeasurement:
        return evaluate_candidate(
            self.config,
            self.content_path,
            self.eval_config,
            self.offensive_ability_ids,
            self.component_index,
            self.mana_cost_index,
            candidate,
        )

    def get_fitness(self, measurement: measurements.AbilityEffectsMeasurement) -> float:
        return measurement.fitness

    def on_candidate(self, measurement: measurements.AbilityEffectsMeasurement) -> None:
        _print_candidate(measurement)

    def on_generation_best(self, generation: int, measurement: measurements.AbilityEffectsMeasurement) -> None:
        _print_generation_best(generation, measurement)


def run_ability_effects_ga(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    component_index: list[ComponentParameter],
    mana_cost_index: list[ComponentParameter],
    initial_candidate: tuple[int, ...],
) -> measurements.AbilityEffectsMeasurement:
    workflow = AbilityEffectsWorkflow(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        component_index,
        mana_cost_index,
        initial_candidate,
    )
    best_key, best_measurement = run_candidate_workflow(workflow)

    # Write the best candidate back to content so downstream stages see it
    apply_candidate_to_content(content_path, best_key, component_index, mana_cost_index)
    return best_measurement


# ── Public API ────────────────────────────────────────────────────────────────

def validate_config(config: config_models.AbilityEffectsBalancerConfig) -> None:
    if config.ga.evaluation_turn_budget <= 0:
        raise ValueError("Ability effects balancer evaluation_turn_budget must be positive.")
    if config.ga.candidate_population_size <= 0 or config.ga.generation_count < 0:
        raise ValueError("Ability effects balancer population config is invalid.")
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Ability effects balancer generated_scenario_count must be positive.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    balance = config.balance

    def _check_pct(name: str, val: float) -> None:
        if not (0.0 < val <= 1.0):
            raise ValueError(
                f"Ability effects config: {name} must be in (0, 1], got {val}."
            )

    _check_pct("component_max_pct_change", balance.component_max_pct_change)
    _check_pct("percent_mod_max_pct_change", balance.percent_mod_max_pct_change)
    _check_pct("flat_mod_max_pct_change", balance.flat_mod_max_pct_change)
    _check_pct("mana_cost_max_pct_change", balance.mana_cost_max_pct_change)

    _check_bounds("pct_change_diversity", balance.pct_change_diversity_target_min, balance.pct_change_diversity_target_max)
    _check_bounds("attacker_win_rate", balance.attacker_win_rate_target_min, balance.attacker_win_rate_target_max)

    weight_sum = (
        balance.win_rate_fitness_weight
        + balance.primary_role_fitness_weight
        + balance.secondary_role_fitness_weight
        + balance.diversity_fitness_weight
    )
    if abs(weight_sum - 1.0) > 1e-6:
        raise ValueError(f"Ability effects fitness weights must sum to 1.0, got {weight_sum:.6f}.")


def _check_bounds(name: str, lo: int | float, hi: int | float) -> None:
    if lo > hi:
        raise ValueError(f"Ability effects config: {name} min ({lo}) > max ({hi}).")


def optimize_ability_effects(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.AbilityEffectsMeasurement:
    validate_config(config)

    components = scenarios.load_effect_components(content_path)
    component_index = build_component_parameter_index(components, config.balance)
    if not component_index:
        raise ValueError("No tunable component parameters found in effectComponentTemplates.json.")

    abilities = scenarios.load_abilities(content_path)
    mana_cost_index = build_mana_cost_parameter_index(abilities, config.balance)

    full_index = component_index + mana_cost_index
    initial_candidate = extract_initial_candidate(full_index)

    print(
        f"ability-effects GA: {len(component_index)} component params + "
        f"{len(mana_cost_index)} mana cost params = {len(full_index)} total, "
        f"pop={config.ga.candidate_population_size}, "
        f"gens={config.ga.generation_count}",
        flush=True,
    )

    return run_ability_effects_ga(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        component_index,
        mana_cost_index,
        initial_candidate,
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

    before = evaluate_initial_ability_effects(config, content_path, eval_config, offensive_ability_ids)
    best = optimize_ability_effects(config, content_path, eval_config, offensive_ability_ids)

    reporting.print_record("best", reporting.ability_effect_fields(best, detailed=True))

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
        # Persist both tuned files back to the source content directory so
        # legacy runs still behave as before. Pipeline package runs disable this.
        scenarios.save_file_to_source_content(
            content_path,
            content_source,
            "effectComponentTemplates.json",
        )
        scenarios.save_file_to_source_content(
            content_path,
            content_source,
            "abilities.json",
        )
        print(f"saved effectComponentTemplates.json + abilities.json to {content_source}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0


def evaluate_initial_ability_effects(
    config: config_models.AbilityEffectsBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.AbilityEffectsMeasurement:
    components = scenarios.load_effect_components(content_path)
    component_index = build_component_parameter_index(components, config.balance)
    abilities = scenarios.load_abilities(content_path)
    mana_cost_index = build_mana_cost_parameter_index(abilities, config.balance)
    initial_candidate = extract_initial_candidate(component_index + mana_cost_index)
    return evaluate_candidate(
        config,
        content_path,
        eval_config,
        offensive_ability_ids,
        component_index,
        mana_cost_index,
        initial_candidate,
    )


def build_package_report(
    before: measurements.AbilityEffectsMeasurement,
    after: measurements.AbilityEffectsMeasurement,
) -> dict:
    return {
        "evidence": reporting.build_evidence_report(
            {"ability-effects": before},
            {"ability-effects": after},
            (
                ("Fitness", "fitness"),
                ("PrimaryRoleScore", "primary_role_score"),
                ("TradeoffScore", "role_tradeoff_score"),
                ("DominanceScore", "role_dominance_score"),
                ("SecondaryRoleScore", "secondary_role_score"),
            ),
        )["evidence"]
    }


# ── Logging helpers ───────────────────────────────────────────────────────────

def _print_candidate(m: measurements.AbilityEffectsMeasurement) -> None:
    reporting.print_record("candidate", reporting.ability_effect_fields(m, detailed=False))


def _print_generation_best(generation: int, m: measurements.AbilityEffectsMeasurement) -> None:
    reporting.print_record(f"generation {generation} best", reporting.ability_effect_fields(m, detailed=True))
