from __future__ import annotations

from collections.abc import Callable

from auto_balancer.config_models.secondary_role_balance_config import SecondaryRoleBalanceConfig
from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from balancing_scripts.primary_roles.common import (
    damage_tradeoff_score,
    healer_tradeoff_score,
    mean,
    role_dominance_score,
    tank_tradeoff_score,
)


def compute_primary_role_value_score(
    config: SecondaryRoleBalanceConfig,
    summary: EvalRoleAlignmentSummary,
) -> float:
    """Score whether the target primary role is fulfilling its value proposition.

    Each primary role has a job that justifies picking it over others:
      Tank   — survives long enough and absorbs enough damage that the HP
               investment is worthwhile. Scored via survival_rate and
               average_damage_taken.
      Healer — heals enough per game that ally survival is meaningfully
               extended. Scored via average_healing_done.
      Damage — deals enough damage per game to justify their lower durability.
               Scored via average_damage_dealt.

    Returns 0.0 if the primary role is None or unrecognised (e.g. standalone
    secondary-role-only runs).
    """
    primary_role = config.target_primary_role
    if not primary_role:
        return 0.0

    units = [
        u for u in summary.units_by_template_id.values()
        if u.primary_role == primary_role
    ]
    if not units:
        return -10.0

    if primary_role == "Tank":
        avg_survival_rate = mean(u.survival_rate for u in units)
        avg_damage_taken = mean(u.average_damage_taken for u in units)
        return mean([
            compute_target_band_fitness(
                avg_survival_rate,
                config.tank_survival_rate_target_min,
                config.tank_survival_rate_target_max,
            ),
            compute_target_band_fitness(
                avg_damage_taken,
                config.tank_average_damage_taken_target_min,
                config.tank_average_damage_taken_target_max,
            ),
            tank_tradeoff_score(summary),
            role_dominance_score(summary),
        ])

    if primary_role == "Healer":
        avg_healing = mean(u.average_healing_done for u in units)
        return mean([
            compute_target_band_fitness(
                avg_healing,
                config.healer_average_healing_done_target_min,
                config.healer_average_healing_done_target_max,
            ),
            healer_tradeoff_score(summary),
            role_dominance_score(summary),
        ])

    if primary_role == "Damage":
        avg_damage_dealt = mean(u.average_damage_dealt for u in units)
        return mean([
            compute_target_band_fitness(
                avg_damage_dealt,
                config.damage_average_damage_dealt_target_min,
                config.damage_average_damage_dealt_target_max,
            ),
            damage_tradeoff_score(summary),
            role_dominance_score(summary),
        ])

    return 0.0


def compute_secondary_role_score(
    target_secondary_role: str,
    target_primary_role: str | None,
    summary: EvalRoleAlignmentSummary,
    candidate_unit_max_hp: int,
    candidate_unit_move_points: int,
    peer_unit_templates: list[dict] | None = None,
) -> float:
    target_units = [
        unit
        for unit in summary.units_by_template_id.values()
        if unit.secondary_role == target_secondary_role
        and (target_primary_role is None or unit.primary_role == target_primary_role)
    ]
    if not target_units:
        return -10.0

    if target_secondary_role == "Buffer":
        return compute_buffer_role_score(summary, target_units)
    if target_secondary_role == "Debuffer":
        return compute_debuffer_role_score(summary, target_units)
    if target_secondary_role == "Acrobat":
        return compute_acrobat_role_score(
            target_primary_role,
            candidate_unit_max_hp,
            candidate_unit_move_points,
            peer_unit_templates or [],
        )

    return 0.0


def compute_acrobat_role_score(
    target_primary_role: str | None,
    candidate_unit_max_hp: int,
    candidate_unit_move_points: int,
    peer_unit_templates: list[dict],
) -> float:
    peer_groups = build_acrobat_peer_groups(target_primary_role, peer_unit_templates)
    if not peer_groups:
        return 1.0

    group_scores: list[float] = []
    for peer_units in peer_groups:
        peer_move_points = mean(int(unit["movePoints"]) for unit in peer_units)
        peer_max_hp = mean(int(unit["maxHP"]) for unit in peer_units)
        if peer_move_points <= 0.0 or peer_max_hp <= 0.0:
            continue

        move_ratio = candidate_unit_move_points / peer_move_points
        hp_ratio = candidate_unit_max_hp / peer_max_hp
        group_scores.append(
            compute_target_band_fitness(move_ratio, 1.15, 1.75) * 0.60
            + compute_target_band_fitness(hp_ratio, 0.55, 0.90) * 0.40
        )

    if not group_scores:
        return 1.0
    return mean(group_scores)


def build_acrobat_peer_groups(
    target_primary_role: str | None,
    peer_unit_templates: list[dict],
) -> list[list[dict]]:
    if target_primary_role is not None:
        return [
            [
                unit
                for unit in peer_unit_templates
                if unit.get("primaryRole") == target_primary_role
                and unit.get("secondaryRole") != "Acrobat"
            ]
        ]

    target_primary_roles = sorted(
        {
            unit.get("primaryRole")
            for unit in peer_unit_templates
            if unit.get("secondaryRole") == "Acrobat" and isinstance(unit.get("primaryRole"), str)
        }
    )
    return [
        [
            unit
            for unit in peer_unit_templates
            if unit.get("primaryRole") == primary_role
            and unit.get("secondaryRole") != "Acrobat"
        ]
        for primary_role in target_primary_roles
    ]


def compute_buffer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    buff_uptime = mean(unit.average_buff_uptime_granted for unit in target_units)
    buff_effects = mean(unit.average_buff_effects_applied for unit in target_units)
    primary_tradeoff = compute_primary_role_tradeoff_score(summary, target_units)

    # Buffers should express buff utility, while the primary-role tradeoff keeps
    # a Tank+Buffer from being as tanky as a non-Buffer Tank.
    return (
        compute_target_band_fitness(buff_uptime, 1.00, 12.00) * 0.45
        + compute_target_band_fitness(buff_effects, 0.50, 6.00) * 0.35
        + primary_tradeoff * 0.20
    )


def compute_debuffer_role_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    debuff_uptime = mean(unit.average_debuff_uptime_granted for unit in target_units)
    debuff_effects = mean(unit.average_debuff_effects_applied for unit in target_units)
    primary_tradeoff = compute_primary_role_tradeoff_score(summary, target_units)

    # Debuffers should express debuff utility, while the primary-role tradeoff
    # keeps the secondary utility from being a free upgrade over a pure primary role.
    return (
        compute_target_band_fitness(debuff_uptime, 1.00, 12.00) * 0.45
        + compute_target_band_fitness(debuff_effects, 0.50, 6.00) * 0.35
        + primary_tradeoff * 0.20
    )


def compute_primary_role_tradeoff_score(
    summary: EvalRoleAlignmentSummary,
    target_units: list[EvalUnitTemplateAggregate],
) -> float:
    primary_scores: list[float] = []
    target_primary_roles = sorted({unit.primary_role for unit in target_units})

    for primary_role in target_primary_roles:
        target_role_units = [unit for unit in target_units if unit.primary_role == primary_role]
        peer_units = [
            unit
            for unit in summary.units_by_template_id.values()
            if unit.primary_role == primary_role
            and unit.secondary_role != target_role_units[0].secondary_role
        ]
        if not peer_units:
            continue

        ratio = compute_primary_power_ratio(primary_role, target_role_units, peer_units)
        if ratio is None:
            continue
        primary_scores.append(compute_target_band_fitness(ratio, 0.55, 0.90))

    if not primary_scores:
        return 1.0
    return mean(primary_scores)


def compute_primary_power_ratio(
    primary_role: str,
    target_units: list[EvalUnitTemplateAggregate],
    peer_units: list[EvalUnitTemplateAggregate],
) -> float | None:
    if primary_role == "Tank":
        ratios = [
            ratio_against_peers(target_units, peer_units, lambda unit: unit.survival_rate),
            ratio_against_peers(target_units, peer_units, lambda unit: unit.average_damage_taken),
        ]
        available_ratios = [ratio for ratio in ratios if ratio is not None]
        if not available_ratios:
            return None
        return mean(available_ratios)
    if primary_role == "Damage":
        return ratio_against_peers(target_units, peer_units, lambda unit: unit.average_damage_dealt)
    if primary_role == "Healer":
        return ratio_against_peers(target_units, peer_units, lambda unit: unit.average_healing_done)

    return None


def ratio_against_peers(
    target_units: list[EvalUnitTemplateAggregate],
    peer_units: list[EvalUnitTemplateAggregate],
    selector: Callable[[EvalUnitTemplateAggregate], float],
) -> float | None:
    peer_value = mean(selector(unit) for unit in peer_units)
    if peer_value <= 0.0:
        return None
    return mean(selector(unit) for unit in target_units) / peer_value
