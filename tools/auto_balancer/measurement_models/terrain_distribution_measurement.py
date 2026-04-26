from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class TerrainDistributionMeasurement:
    mountain_tile_percent: int
    water_tile_percent: int
    plain_tile_percent: int
    attacker_wins: int
    turn_limit_count: int
    total_runs: int
    attacker_win_rate: float
    turn_limit_rate: float
    average_attacker_turn_count: float
    average_action_count: float
    move_action_rate: float
    skip_action_rate: float
    ability_use_rate: float
    offensive_ability_use_rate: float
    support_ability_use_rate: float
    raw_fitness: float
    fitness: float
    attacker_win_rate_confidence_min: float
    attacker_win_rate_confidence_max: float
    error_message: str | None
