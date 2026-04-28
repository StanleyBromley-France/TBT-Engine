from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class AbilityEffectsMeasurement:
    # Overall match balance
    attacker_win_rate: float

    # Aggregate per-role combat metrics (averages over all units of that role)
    average_tank_damage_dealt: float
    average_healer_healing_done: float
    average_damage_damage_dealt: float
    average_buffer_buff_uptime: float
    average_debuffer_debuff_uptime: float

    # Observed spread of fractional % changes across all tuned parameters
    pct_change_std_dev: float

    # Fitness component scores
    win_rate_score: float
    primary_role_score: float
    role_tradeoff_score: float
    role_dominance_score: float
    secondary_role_score: float
    diversity_score: float
    fitness: float
    error_message: str | None
