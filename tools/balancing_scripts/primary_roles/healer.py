from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import mean, safe_ratio


def compute_healer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    healing_done = mean(unit.average_healing_done for unit in target_units)
    damage_dealt = mean(unit.average_damage_dealt for unit in target_units)
    survival = mean(unit.survival_rate for unit in target_units)

    non_healer_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role != "Healer"]
    damage_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role == "Damage"]
    tank_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role == "Tank"]

    healing_vs_non_healer = safe_ratio(
        healing_done,
        mean(unit.average_healing_done for unit in non_healer_units),
    )
    damage_vs_damage = safe_ratio(
        damage_dealt,
        mean(unit.average_damage_dealt for unit in damage_units),
    )
    survival_vs_tank = safe_ratio(
        survival,
        mean(unit.survival_rate for unit in tank_units),
    )

    return (
        compute_target_band_fitness(healing_vs_non_healer, 1.50, 5.00) * 0.45
        + compute_target_band_fitness(damage_vs_damage, 0.15, 0.85) * 0.25
        + compute_target_band_fitness(survival_vs_tank, 0.45, 1.10) * 0.30
    )
