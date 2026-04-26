from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary
from balancing_scripts.primary_roles.damage import compute_damage_role_score
from balancing_scripts.primary_roles.healer import compute_healer_role_score
from balancing_scripts.primary_roles.tank import compute_tank_role_score


def compute_primary_role_score(target_primary_role: str, summary: EvalRoleAlignmentSummary) -> float:
    target_units = [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.primary_role == target_primary_role
    ]
    if not target_units:
        return -10.0

    if target_primary_role == "Tank":
        return compute_tank_role_score(summary, target_units)
    if target_primary_role == "Damage":
        return compute_damage_role_score(summary, target_units)
    if target_primary_role == "Healer":
        return compute_healer_role_score(summary, target_units)

    return 0.0
