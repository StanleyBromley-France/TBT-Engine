from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import mean, safe_ratio


def compute_damage_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    damage_dealt = mean(unit.average_damage_dealt for unit in target_units)
    survival = mean(unit.survival_rate for unit in target_units)

    non_damage_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role != "Damage"]
    tank_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role == "Tank"]
    healer_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role == "Healer"]

    damage_vs_non_damage = safe_ratio(
        damage_dealt,
        mean(unit.average_damage_dealt for unit in non_damage_units),
    )
    damage_vs_healer = safe_ratio(
        damage_dealt,
        mean(unit.average_damage_dealt for unit in healer_units),
    )
    survival_vs_tank = safe_ratio(
        survival,
        mean(unit.survival_rate for unit in tank_units),
    )

    return (
        compute_target_band_fitness(damage_vs_non_damage, 1.20, 2.80) * 0.45
        + compute_target_band_fitness(damage_vs_healer, 1.20, 3.00) * 0.30
        + compute_target_band_fitness(survival_vs_tank, 0.35, 0.95) * 0.25
    )
