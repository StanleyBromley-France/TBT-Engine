from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from balancing_scripts.primary_roles.common import (
    healer_tradeoff_score,
    mean,
    role_dominance_score,
    safe_ratio,
    score_at_least,
    score_at_most,
)


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
        score_at_least(healing_vs_non_healer, 1.50) * 0.30
        + score_at_most(damage_vs_damage, 0.85) * 0.15
        + score_at_most(survival_vs_tank, 1.10) * 0.15
        + healer_tradeoff_score(summary) * 0.25
        + role_dominance_score(summary) * 0.15
    )
