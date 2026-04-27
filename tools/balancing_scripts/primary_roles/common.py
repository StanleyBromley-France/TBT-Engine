from __future__ import annotations

from typing import Iterable

from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate


def mean(values: Iterable[float]) -> float:
    values_list = list(values)
    if not values_list:
        return 0.0
    return sum(values_list) / len(values_list)


def safe_ratio(numerator: float, denominator: float) -> float:
    if denominator <= 0.0:
        return 0.0 if numerator <= 0.0 else numerator
    return numerator / denominator


def score_at_most(observed_value: float, target_max: float) -> float:
    if observed_value <= target_max:
        return 1.0
    return 1.0 - ((observed_value - target_max) * 4.0)


def score_at_least(observed_value: float, target_min: float) -> float:
    if observed_value >= target_min:
        return 1.0
    return 1.0 - ((target_min - observed_value) * 4.0)


def units_for_primary_role(
    summary: EvalRoleAlignmentSummary,
    primary_role: str,
) -> list[EvalUnitTemplateAggregate]:
    return [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.primary_role == primary_role
    ]


def effective_durability(units: list[EvalUnitTemplateAggregate]) -> float:
    """Approximate practical durability from existing eval telemetry.

    This intentionally uses only tool-side data. Damage taken captures pressure
    absorbed, turns survived captures battlefield presence, and survival rate
    keeps end-state resilience visible even when a unit was not heavily focused.
    """
    if not units:
        return 0.0
    return (
        mean(unit.average_damage_taken for unit in units)
        + (mean(unit.average_turns_survived for unit in units) * 8.0)
        + (mean(unit.survival_rate for unit in units) * 40.0)
    )


def primary_role_metrics(
    summary: EvalRoleAlignmentSummary,
    primary_role: str,
) -> dict[str, float]:
    units = units_for_primary_role(summary, primary_role)
    return {
        "damage": mean(unit.average_damage_dealt for unit in units),
        "healing": mean(unit.average_healing_done for unit in units),
        "damage_taken": mean(unit.average_damage_taken for unit in units),
        "survival": mean(unit.survival_rate for unit in units),
        "turns_survived": mean(unit.average_turns_survived for unit in units),
        "durability": effective_durability(units),
    }


def tank_tradeoff_score(summary: EvalRoleAlignmentSummary) -> float:
    tank = primary_role_metrics(summary, "Tank")
    damage = primary_role_metrics(summary, "Damage")
    if tank["durability"] <= 0.0 or damage["durability"] <= 0.0:
        return 0.0

    durability_gain = safe_ratio(tank["durability"], damage["durability"])
    damage_cost = safe_ratio(damage["damage"], max(tank["damage"], 1.0))
    tradeoff_ratio = safe_ratio(durability_gain, damage_cost)

    # If tanks sacrifice a lot of total damage, they need a matching durability
    # advantage. Also prevent "tank damage is close enough to damage dealers"
    # from passing just because durability is high.
    return (
        score_at_least(tradeoff_ratio, 0.85) * 0.70
        + score_at_most(safe_ratio(tank["damage"], max(damage["damage"], 1.0)), 0.75) * 0.30
    )


def damage_tradeoff_score(summary: EvalRoleAlignmentSummary) -> float:
    damage = primary_role_metrics(summary, "Damage")
    tank = primary_role_metrics(summary, "Tank")
    healer = primary_role_metrics(summary, "Healer")
    if damage["durability"] <= 0.0:
        return 0.0

    offense_gain_vs_tank = safe_ratio(damage["damage"], max(tank["damage"], 1.0))
    offense_gain_vs_healer = safe_ratio(damage["damage"], max(healer["damage"], 1.0))
    fragility_cost = safe_ratio(tank["durability"], damage["durability"])
    tradeoff_ratio = safe_ratio(offense_gain_vs_tank, fragility_cost)

    return (
        score_at_least(tradeoff_ratio, 0.85) * 0.55
        + score_at_least(offense_gain_vs_healer, 1.35) * 0.25
        + score_at_most(safe_ratio(damage["durability"], max(tank["durability"], 1.0)), 0.85) * 0.20
    )


def healer_tradeoff_score(summary: EvalRoleAlignmentSummary) -> float:
    healer = primary_role_metrics(summary, "Healer")
    damage = primary_role_metrics(summary, "Damage")
    tank = primary_role_metrics(summary, "Tank")
    if healer["healing"] <= 0.0:
        return 0.0

    healing_vs_damage_output = safe_ratio(healer["healing"], max(damage["damage"], 1.0))
    damage_cost = safe_ratio(damage["damage"], max(healer["damage"], 1.0))
    durability_cost = safe_ratio(tank["durability"], max(healer["durability"], 1.0))
    total_cost = (damage_cost + durability_cost) / 2.0
    tradeoff_ratio = safe_ratio(healing_vs_damage_output, total_cost)

    return (
        score_at_least(tradeoff_ratio, 0.45) * 0.55
        + score_at_least(healer["healing"], damage["damage"] * 0.45) * 0.25
        + score_at_most(safe_ratio(healer["damage"], max(damage["damage"], 1.0)), 0.70) * 0.20
    )


def role_dominance_score(summary: EvalRoleAlignmentSummary) -> float:
    tank = primary_role_metrics(summary, "Tank")
    damage = primary_role_metrics(summary, "Damage")
    healer = primary_role_metrics(summary, "Healer")

    scores = [
        # Damage dealers should not keep nearly tank-level practical durability.
        score_at_most(safe_ratio(damage["durability"], max(tank["durability"], 1.0)), 0.85),
        # Tanks should not approach damage dealers' total offensive output.
        score_at_most(safe_ratio(tank["damage"], max(damage["damage"], 1.0)), 0.75),
        # Healers should not become a free all-rounder role.
        score_at_most(safe_ratio(healer["damage"], max(damage["damage"], 1.0)), 0.70),
        score_at_most(safe_ratio(healer["durability"], max(tank["durability"], 1.0)), 0.90),
        # If everyone is close to tank durability, healer value is less distinct.
        score_at_most(
            safe_ratio((damage["durability"] + healer["durability"]) / 2.0, max(tank["durability"], 1.0)),
            0.88,
        ),
    ]
    return mean(scores)
