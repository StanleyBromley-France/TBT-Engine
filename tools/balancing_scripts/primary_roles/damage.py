from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from balancing_scripts.primary_roles.common import (
    damage_tradeoff_score,
    mean,
    primary_role_metrics,
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

    match_score = (
        score_at_least(damage_vs_non_damage, 1.20) * 0.30
        + score_at_least(damage_vs_healer, 1.20) * 0.20
        + score_at_most(survival_vs_tank, 0.95) * 0.15
        + damage_tradeoff_score(summary) * 0.25
        + role_dominance_score(summary) * 0.10
    )
    turn_score = compute_damage_per_turn_score(summary)
    return (match_score * 0.70) + (turn_score * 0.30)


def compute_damage_per_turn_score(summary: EvalRoleAlignmentSummary) -> float:
    damage = primary_role_metrics(summary, "Damage")
    tank = primary_role_metrics(summary, "Tank")
    healer = primary_role_metrics(summary, "Healer")
    non_damage_damage_per_turn = mean([
        tank["damage_per_turn"],
        healer["damage_per_turn"],
    ])

    return (
        score_at_least(
            safe_ratio(damage["damage_per_turn"], non_damage_damage_per_turn),
            1.25,
        ) * 0.65
        + score_at_most(
            safe_ratio(damage["damage_taken_per_turn"], max(tank["damage_taken_per_turn"], 1.0)),
            0.85,
        ) * 0.35
    )
