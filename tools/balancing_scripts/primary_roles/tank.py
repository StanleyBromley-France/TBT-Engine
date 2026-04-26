from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import mean, safe_ratio


def compute_tank_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    tank_survival = mean(unit.survival_rate for unit in target_units)
    tank_damage_taken = mean(unit.average_damage_taken for unit in target_units)
    tank_damage_dealt = mean(unit.average_damage_dealt for unit in target_units)

    non_tank_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role != "Tank"]
    damage_units = [unit for unit in summary.units_by_template_id.values() if unit.primary_role == "Damage"]

    survival_vs_non_tank = safe_ratio(tank_survival, mean(unit.survival_rate for unit in non_tank_units))
    damage_taken_vs_non_tank = safe_ratio(
        tank_damage_taken,
        mean(unit.average_damage_taken for unit in non_tank_units),
    )
    damage_dealt_vs_damage = safe_ratio(
        tank_damage_dealt,
        mean(unit.average_damage_dealt for unit in damage_units),
    )

    return (
        compute_target_band_fitness(survival_vs_non_tank, 1.10, 1.80) * 0.45
        + compute_target_band_fitness(damage_taken_vs_non_tank, 1.10, 2.20) * 0.35
        + compute_target_band_fitness(damage_dealt_vs_damage, 0.25, 0.90) * 0.20
    )
