from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from balancing_scripts.primary_roles.common import (
    mean,
    primary_role_metrics,
    role_dominance_score,
    safe_ratio,
    score_at_least,
    score_at_most,
    tank_tradeoff_score,
)


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

    match_score = (
        score_at_least(survival_vs_non_tank, 1.10) * 0.25
        + score_at_least(damage_taken_vs_non_tank, 1.10) * 0.20
        + score_at_most(damage_dealt_vs_damage, 0.90) * 0.15
        + tank_tradeoff_score(summary) * 0.25
        + role_dominance_score(summary) * 0.15
    )
    turn_score = compute_tank_per_turn_score(summary)
    return (match_score * 0.70) + (turn_score * 0.30)


def compute_tank_per_turn_score(summary: EvalRoleAlignmentSummary) -> float:
    tank = primary_role_metrics(summary, "Tank")
    damage = primary_role_metrics(summary, "Damage")
    healer = primary_role_metrics(summary, "Healer")
    non_tank_damage_taken_per_turn = mean([
        damage["damage_taken_per_turn"],
        healer["damage_taken_per_turn"],
    ])

    return (
        score_at_least(
            safe_ratio(tank["damage_taken_per_turn"], non_tank_damage_taken_per_turn),
            1.20,
        ) * 0.60
        + score_at_most(
            safe_ratio(tank["damage_per_turn"], max(damage["damage_per_turn"], 1.0)),
            0.75,
        ) * 0.40
    )
