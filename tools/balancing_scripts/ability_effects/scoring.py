from __future__ import annotations

import statistics

from auto_balancer.config_models.ability_effects_config import AbilityEffectsBalanceConfig
from auto_balancer.eval.results import EvalRoleAlignmentSummary, EvalUnitTemplateAggregate
from auto_balancer.ga.fitness import compute_target_band_fitness
from auto_balancer.measurement_models.ability_effects_measurement import AbilityEffectsMeasurement
from balancing_scripts.primary_roles.common import (
    damage_tradeoff_score,
    healer_tradeoff_score,
    mean,
    role_dominance_score,
    tank_tradeoff_score,
)


def compute_ability_effects_score(
    config: AbilityEffectsBalanceConfig,
    summary: EvalRoleAlignmentSummary,
    pct_changes: list[float],
) -> AbilityEffectsMeasurement:
    """Score a candidate on four axes.

    win_rate
        Global attacker win rate — should be near 50 %. Catches ability values
        that make the game one-sided.

    primary_role
        Per-role output bands: tanks deal moderate damage, healers heal
        meaningfully, damage dealers output high damage.

    secondary_role
        Per-secondary output bands: buffers show buff uptime, debuffers show
        debuff uptime.

    diversity
        Standard deviation of fractional changes (final − initial) / initial
        applied across all tuned parameters. Rewards the GA for applying
        varied corrections — some parameters pushed up, some down, some left
        near baseline — rather than uniformly boosting or nerfing everything.
    """
    units = list(summary.units_by_template_id.values())
    detailed = summary.detailed

    # ── Win rate ──────────────────────────────────────────────────────────────
    attacker_win_rate = (
        detailed.attacker_wins / detailed.total_runs if detailed.total_runs > 0 else 0.5
    )
    win_rate_score = compute_target_band_fitness(
        attacker_win_rate,
        config.attacker_win_rate_target_min,
        config.attacker_win_rate_target_max,
    )

    # ── Primary role scoring ──────────────────────────────────────────────────
    tank_units = [u for u in units if u.primary_role == "Tank"]
    healer_units = [u for u in units if u.primary_role == "Healer"]
    damage_units = [u for u in units if u.primary_role == "Damage"]

    avg_tank_damage = _safe_mean(tank_units, lambda u: u.average_damage_dealt)
    avg_healer_healing = _safe_mean(healer_units, lambda u: u.average_healing_done)
    avg_damage_damage = _safe_mean(damage_units, lambda u: u.average_damage_dealt)

    role_output_score = mean([
        compute_target_band_fitness(
            avg_tank_damage,
            config.tank_average_damage_dealt_target_min,
            config.tank_average_damage_dealt_target_max,
        ),
        compute_target_band_fitness(
            avg_healer_healing,
            config.healer_average_healing_done_target_min,
            config.healer_average_healing_done_target_max,
        ),
        compute_target_band_fitness(
            avg_damage_damage,
            config.damage_average_damage_dealt_target_min,
            config.damage_average_damage_dealt_target_max,
        ),
    ])
    role_tradeoff_score = mean([
        tank_tradeoff_score(summary),
        healer_tradeoff_score(summary),
        damage_tradeoff_score(summary),
    ])
    dominance_score = role_dominance_score(summary)
    primary_role_score = (
        role_output_score * 0.55
        + role_tradeoff_score * 0.30
        + dominance_score * 0.15
    )

    # ── Secondary role scoring ────────────────────────────────────────────────
    buffer_units = [u for u in units if u.secondary_role == "Buffer"]
    debuffer_units = [u for u in units if u.secondary_role == "Debuffer"]

    avg_buffer_uptime = _safe_mean(buffer_units, lambda u: u.average_buff_uptime_granted)
    avg_debuffer_uptime = _safe_mean(debuffer_units, lambda u: u.average_debuff_uptime_granted)

    secondary_role_score = mean([
        compute_target_band_fitness(
            avg_buffer_uptime,
            config.buffer_average_buff_uptime_target_min,
            config.buffer_average_buff_uptime_target_max,
        ),
        compute_target_band_fitness(
            avg_debuffer_uptime,
            config.debuffer_average_debuff_uptime_target_min,
            config.debuffer_average_debuff_uptime_target_max,
        ),
    ])

    # ── Diversity scoring ─────────────────────────────────────────────────────
    # Measure how varied the applied % corrections are.  A uniform +10% across
    # all parameters scores poorly; a mix of −20%, 0%, +15% scores well.
    pct_std = statistics.pstdev(pct_changes) if len(pct_changes) >= 2 else 0.0
    diversity_score = compute_target_band_fitness(
        pct_std,
        config.pct_change_diversity_target_min,
        config.pct_change_diversity_target_max,
    )

    fitness = (
        win_rate_score * config.win_rate_fitness_weight
        + primary_role_score * config.primary_role_fitness_weight
        + secondary_role_score * config.secondary_role_fitness_weight
        + diversity_score * config.diversity_fitness_weight
    )

    return AbilityEffectsMeasurement(
        attacker_win_rate=attacker_win_rate,
        average_tank_damage_dealt=avg_tank_damage,
        average_healer_healing_done=avg_healer_healing,
        average_damage_damage_dealt=avg_damage_damage,
        average_buffer_buff_uptime=avg_buffer_uptime,
        average_debuffer_debuff_uptime=avg_debuffer_uptime,
        pct_change_std_dev=pct_std,
        win_rate_score=win_rate_score,
        primary_role_score=primary_role_score,
        role_tradeoff_score=role_tradeoff_score,
        role_dominance_score=dominance_score,
        secondary_role_score=secondary_role_score,
        diversity_score=diversity_score,
        fitness=fitness,
        error_message=None,
    )


def _safe_mean(
    units: list[EvalUnitTemplateAggregate],
    selector,
) -> float:
    if not units:
        return 0.0
    return mean(selector(u) for u in units)
