from __future__ import annotations

from collections.abc import Callable

from auto_balancer.config_models.secondary_role_balance_config import SecondaryRoleBalanceConfig
from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import mean
from balancing_scripts.primary_roles.scoring import compute_primary_role_score


def compute_role_combination_score(
    config: SecondaryRoleBalanceConfig,
    summary: EvalRoleAlignmentSummary,
    candidate_unit_max_hp: int,
    candidate_unit_move_points: int,
    peer_unit_templates: list[dict] | None = None,
) -> float:
    """Score a hybrid role using family balance plus a pure-primary limit.

    The family balance reuses the same primary-role relationship scoring used
    by the primary-role baseline balancer, but filters the summary to the
    current secondary role. For example, the Buffer family is scored as
    Tank+Buffer vs Healer+Buffer vs Damage+Buffer.

    The pure-primary limit is the extra hybrid constraint: Tank+Buffer should
    not be as tanky as pure Tank, Damage+Debuffer should not deal as much damage
    as pure Damage, and so on.
    """
    primary_role = config.target_primary_role
    secondary_role = config.target_secondary_role
    if not primary_role or not secondary_role:
        return -10.0

    target_units = [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.primary_role == primary_role
        and unit.secondary_role == secondary_role
    ]
    if not target_units:
        return -10.0

    pure_primary_units = [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.primary_role == primary_role
        and unit.secondary_role is None
    ]
    family_primary_score = compute_family_primary_role_score(summary, secondary_role)
    primary_anchor_score = compute_primary_anchor_score(primary_role, target_units, pure_primary_units)
    secondary_expression_score = compute_secondary_expression_score(summary, secondary_role, target_units)
    win_rate_score = compute_role_combination_win_rate_score(summary, primary_role, secondary_role)

    return mean([family_primary_score, primary_anchor_score, secondary_expression_score, win_rate_score])


def compute_family_primary_role_score(
    summary: EvalRoleAlignmentSummary,
    secondary_role: str,
) -> float:
    family_units = {
        unit_id: unit
        for unit_id, unit in summary.units_by_template_id.items()
        if unit.secondary_role == secondary_role
    }
    if not family_units:
        return -10.0

    family_summary = EvalRoleAlignmentSummary(summary.detailed, family_units, summary.role_combination_win_rates)
    return mean([
        compute_primary_role_score("Tank", family_summary),
        compute_primary_role_score("Healer", family_summary),
        compute_primary_role_score("Damage", family_summary),
    ])


def compute_role_combination_win_rate_score(
    summary: EvalRoleAlignmentSummary,
    primary_role: str,
    secondary_role: str,
) -> float:
    role_combination = f"{primary_role}+{secondary_role}"
    aggregate = summary.role_combination_win_rates.get(role_combination)
    if aggregate is None or aggregate.team_observations <= 0:
        return -10.0

    fair_score = compute_target_band_fitness(aggregate.win_rate, 0.45, 0.55)

    family_win_rates = [
        item.win_rate
        for item in summary.role_combination_win_rates.values()
        if item.secondary_role == secondary_role and item.team_observations > 0
    ]
    family_average = mean(family_win_rates)
    if family_average <= 0.0:
        return fair_score

    relative_score = compute_target_band_fitness(aggregate.win_rate / family_average, 0.85, 1.15)
    return mean([fair_score, relative_score])


def compute_secondary_expression_score(
    summary: EvalRoleAlignmentSummary,
    secondary_role: str,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    if secondary_role == "Buffer":
        buff_uptime = mean(unit.average_buff_uptime_granted for unit in target_units)
        buff_effects = mean(unit.average_buff_effects_applied for unit in target_units)
        return mean([
            compute_target_band_fitness(buff_uptime, 1.00, 12.00),
            compute_target_band_fitness(buff_effects, 0.50, 6.00),
        ])

    if secondary_role == "Debuffer":
        debuff_uptime = mean(unit.average_debuff_uptime_granted for unit in target_units)
        debuff_effects = mean(unit.average_debuff_effects_applied for unit in target_units)
        return mean([
            compute_target_band_fitness(debuff_uptime, 1.00, 12.00),
            compute_target_band_fitness(debuff_effects, 0.50, 6.00),
        ])

    if secondary_role == "Acrobat":
        target_tiles_moved = mean(unit.average_tiles_moved_total for unit in target_units)
        non_acrobat_tiles_moved = mean(
            unit.average_tiles_moved_total
            for unit in summary.units_by_template_id.values()
            if unit.secondary_role != "Acrobat"
        )
        if non_acrobat_tiles_moved <= 0.0:
            return compute_target_band_fitness(target_tiles_moved, 4.00, 20.00)
        return compute_target_band_fitness(target_tiles_moved / non_acrobat_tiles_moved, 1.15, 1.75)

    return -10.0


def compute_primary_anchor_score(
    primary_role: str,
    target_units: list[EvalUnitTemplateAggregate],
    pure_primary_units: list[EvalUnitTemplateAggregate],
) -> float:
    if not pure_primary_units:
        return -10.0

    ratio = compute_primary_power_ratio(primary_role, target_units, pure_primary_units)
    if ratio is None:
        return -10.0

    return compute_target_band_fitness(ratio, 0.55, 0.90)


def compute_primary_power_ratio(
    primary_role: str,
    target_units: list[EvalUnitTemplateAggregate],
    peer_units: list[EvalUnitTemplateAggregate],
) -> float | None:
    if primary_role == "Tank":
        ratios = [
            ratio_against_peers(target_units, peer_units, lambda unit: unit.survival_rate),
            ratio_against_peers(target_units, peer_units, lambda unit: unit.average_damage_taken),
        ]
        available_ratios = [ratio for ratio in ratios if ratio is not None]
        if not available_ratios:
            return None
        return mean(available_ratios)
    if primary_role == "Damage":
        return ratio_against_peers(target_units, peer_units, lambda unit: unit.average_damage_dealt)
    if primary_role == "Healer":
        return ratio_against_peers(target_units, peer_units, lambda unit: unit.average_healing_done)

    return None


def ratio_against_peers(
    target_units: list[EvalUnitTemplateAggregate],
    peer_units: list[EvalUnitTemplateAggregate],
    selector: Callable[[EvalUnitTemplateAggregate], float],
) -> float | None:
    peer_value = mean(selector(unit) for unit in peer_units)
    if peer_value <= 0.0:
        return None
    return mean(selector(unit) for unit in target_units) / peer_value
