from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class CombinedPrimaryRoleMeasurement:
    tank_unit_max_hp: int
    tank_unit_max_mana_points: int
    tank_unit_move_points: int
    tank_physical_damage_received_percent: int
    tank_magic_damage_received_percent: int
    damage_unit_max_hp: int
    damage_unit_max_mana_points: int
    damage_unit_move_points: int
    damage_physical_damage_received_percent: int
    damage_magic_damage_received_percent: int
    healer_unit_max_hp: int
    healer_unit_max_mana_points: int
    healer_unit_move_points: int
    healer_physical_damage_received_percent: int
    healer_magic_damage_received_percent: int
    attacker_win_rate: float
    turn_limit_rate: float
    average_attacker_turn_count: float
    average_action_count: float
    tank_primary_role_score: float
    damage_primary_role_score: float
    healer_primary_role_score: float
    average_primary_role_score: float
    raw_fitness: float
    fitness: float
    error_message: str | None
