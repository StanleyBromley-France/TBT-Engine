from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class PrimaryRoleMeasurement:
    unit_max_hp: int
    unit_max_mana_points: int
    unit_move_points: int
    physical_damage_received_percent: int
    magic_damage_received_percent: int
    attacker_win_rate: float
    turn_limit_rate: float
    average_attacker_turn_count: float
    average_action_count: float
    primary_role_alignment_score: float
    raw_fitness: float
    fitness: float
    error_message: str | None
