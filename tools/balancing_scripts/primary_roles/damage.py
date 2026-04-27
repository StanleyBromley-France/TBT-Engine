from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from balancing_scripts.primary_roles.common import (
    damage_tradeoff_score,
    mean,
    role_dominance_score,
    safe_ratio,
    score_at_least,
    score_at_most,
)


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
        score_at_least(damage_vs_non_damage, 1.20) * 0.30
        + score_at_least(damage_vs_healer, 1.20) * 0.20
        + score_at_most(survival_vs_tank, 0.95) * 0.15
        + damage_tradeoff_score(summary) * 0.25
        + role_dominance_score(summary) * 0.10
    )
