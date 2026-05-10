from __future__ import annotations

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from balancing_scripts.primary_roles.common import (
    mean,
    primary_role_metrics,
    safe_ratio,
)


def compute_healer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    if not target_units:
        return -10.0
    return compute_healer_role_diagnostics(summary)["score"]


def compute_healer_per_turn_score(summary: EvalRoleAlignmentSummary) -> float:
    diagnostics = compute_healer_role_diagnostics(summary)
    return (
        diagnostics["healing_efficiency_score"] * 0.50
        + diagnostics["low_damage_score"] * 0.25
        + diagnostics["damage_taken_score"] * 0.25
    )


def compute_healer_role_diagnostics(summary: EvalRoleAlignmentSummary) -> dict[str, float]:
    healer = primary_role_metrics(summary, "Healer")
    damage = primary_role_metrics(summary, "Damage")
    tank = primary_role_metrics(summary, "Tank")
    all_units = list(summary.units_by_template_id.values())
    all_units_damage_taken = mean(unit.average_damage_taken for unit in all_units)

    healing_presence_ratio = safe_ratio(healer["healing"], all_units_damage_taken)
    healing_efficiency_ratio = safe_ratio(healer["healing_per_turn"], max(damage["damage_per_turn"], 1.0))
    damage_ratio = safe_ratio(healer["damage_per_turn"], max(damage["damage_per_turn"], 1.0))
    durability_ratio = safe_ratio(healer["durability"], max(tank["durability"], 1.0))
    damage_taken_ratio = safe_ratio(healer["damage_taken_per_turn"], max(tank["damage_taken_per_turn"], 1.0))
    tradeoff_ratio = safe_ratio(healer["healing"], max(healer["damage"], 1.0))

    healing_presence_score = score_target_band(healing_presence_ratio, 0.20, 0.90)
    healing_efficiency_score = score_target_band(healing_efficiency_ratio, 0.25, 0.80)
    low_damage_score = score_at_most_clamped(damage_ratio, 0.70)
    durability_score = score_target_band(durability_ratio, 0.35, 0.95)
    damage_taken_score = score_at_most_clamped(damage_taken_ratio, 0.90)
    tradeoff_score = score_at_least_clamped(tradeoff_ratio, 1.25)

    identity_score = (
        healing_presence_score * 0.35
        + healing_efficiency_score * 0.25
        + low_damage_score * 0.15
        + durability_score * 0.15
        + tradeoff_score * 0.10
    )

    return {
        "score": clamp_score(identity_score),
        "healing_presence_ratio": healing_presence_ratio,
        "healing_efficiency_ratio": healing_efficiency_ratio,
        "damage_ratio": damage_ratio,
        "durability_ratio": durability_ratio,
        "damage_taken_ratio": damage_taken_ratio,
        "tradeoff_ratio": tradeoff_ratio,
        "healing_presence_score": healing_presence_score,
        "healing_efficiency_score": healing_efficiency_score,
        "low_damage_score": low_damage_score,
        "durability_score": durability_score,
        "damage_taken_score": damage_taken_score,
        "tradeoff_score": tradeoff_score,
    }


def score_target_band(observed_value: float, target_min: float, target_max: float) -> float:
    if observed_value < target_min:
        return score_at_least_clamped(observed_value, target_min)
    if observed_value > target_max:
        return score_at_most_clamped(observed_value, target_max)
    return 1.0


def score_at_least_clamped(observed_value: float, target_min: float) -> float:
    if observed_value >= target_min:
        return 1.0
    return clamp_score(1.0 - ((target_min - observed_value) * 4.0))


def score_at_most_clamped(observed_value: float, target_max: float) -> float:
    if observed_value <= target_max:
        return 1.0
    return clamp_score(1.0 - ((observed_value - target_max) * 4.0))


def clamp_score(score: float) -> float:
    return max(-10.0, min(1.0, score))
