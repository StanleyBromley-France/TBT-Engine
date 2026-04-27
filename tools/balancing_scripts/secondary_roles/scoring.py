from __future__ import annotations

from collections.abc import Callable

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import mean


def compute_secondary_role_score(
    target_secondary_role: str,
    target_primary_role: str | None,
    summary: EvalRoleAlignmentSummary,
) -> float:
    target_units = [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.secondary_role == target_secondary_role
        and (target_primary_role is None or unit.primary_role == target_primary_role)
    ]
    if not target_units:
        return -10.0

    if target_secondary_role == "Buffer":
        return compute_buffer_role_score(summary, target_units)
    if target_secondary_role == "Debuffer":
        return compute_debuffer_role_score(summary, target_units)

    return 0.0


def compute_buffer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    buff_uptime = mean(unit.average_buff_uptime_granted for unit in target_units)
    buff_effects = mean(unit.average_buff_effects_applied for unit in target_units)
    debuff_uptime = mean(unit.average_debuff_uptime_granted for unit in target_units)
    debuff_effects = mean(unit.average_debuff_effects_applied for unit in target_units)
    primary_tradeoff = compute_primary_role_tradeoff_score(summary, target_units)

    # Secondary roles are exclusive: Buffer output should be buff output, not
    # merely "more buff output than everyone else". The primary-role tradeoff
    # keeps a Tank+Buffer from being as tanky as a non-Buffer Tank.
    return (
        compute_target_band_fitness(buff_uptime, 1.00, 12.00) * 0.34
        + compute_target_band_fitness(buff_effects, 0.50, 6.00) * 0.26
        + compute_target_band_fitness(debuff_uptime, 0.00, 0.05) * 0.075
        + compute_target_band_fitness(debuff_effects, 0.00, 0.05) * 0.075
        + primary_tradeoff * 0.25
    )


def compute_debuffer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    debuff_uptime = mean(unit.average_debuff_uptime_granted for unit in target_units)
    debuff_effects = mean(unit.average_debuff_effects_applied for unit in target_units)
    buff_uptime = mean(unit.average_buff_uptime_granted for unit in target_units)
    buff_effects = mean(unit.average_buff_effects_applied for unit in target_units)
    primary_tradeoff = compute_primary_role_tradeoff_score(summary, target_units)

    # Secondary roles are exclusive: Debuffer output should be debuff output,
    # with buff output treated as role leakage. The primary-role tradeoff keeps
    # the secondary utility from being a free upgrade over a pure primary role.
    return (
        compute_target_band_fitness(debuff_uptime, 1.00, 12.00) * 0.34
        + compute_target_band_fitness(debuff_effects, 0.50, 6.00) * 0.26
        + compute_target_band_fitness(buff_uptime, 0.00, 0.05) * 0.075
        + compute_target_band_fitness(buff_effects, 0.00, 0.05) * 0.075
        + primary_tradeoff * 0.25
    )


def compute_primary_role_tradeoff_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    primary_scores: list[float] = []
    target_primary_roles = sorted({unit.primary_role for unit in target_units})

    for primary_role in target_primary_roles:
        target_role_units = [unit for unit in target_units if unit.primary_role == primary_role]
        peer_units = [
            unit
            for unit in summary.units_by_template_id.values()
            if unit.primary_role == primary_role
            and unit.secondary_role != target_role_units[0].secondary_role
        ]
        if not peer_units:
            continue

        ratio = compute_primary_power_ratio(primary_role, target_role_units, peer_units)
        if ratio is None:
            continue
        primary_scores.append(compute_target_band_fitness(ratio, 0.55, 0.90))

    if not primary_scores:
        return 1.0
    return mean(primary_scores)


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
