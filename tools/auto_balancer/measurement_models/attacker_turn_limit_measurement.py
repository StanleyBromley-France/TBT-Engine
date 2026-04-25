from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class AttackerTurnLimitMeasurement:
    turn_budget: int
    attacker_wins: int
    turn_limit_count: int
    total_runs: int
    attacker_win_rate: float
    turn_limit_rate: float
    raw_fitness: float
    fitness: float
    attacker_win_rate_confidence_min: float
    attacker_win_rate_confidence_max: float
