from __future__ import annotations

from dataclasses import dataclass


FullGenomeCandidate = tuple[int, ...]
UnitProfileModifierMap = dict[str, tuple[int, int, int, int, int]]
AbilityGroupMultiplierMap = dict[str, int]


@dataclass(frozen=True)
class FullGenomeMeasurement:
    candidate: FullGenomeCandidate
    unit_profile_modifiers: UnitProfileModifierMap
    ability_group_multipliers: AbilityGroupMultiplierMap

    attacker_win_rate: float
    turn_limit_rate: float
    average_attacker_turn_count: float

    match_flow_score: float
    primary_role_identity_score: float
    secondary_role_identity_score: float
    role_profile_fairness_score: float
    change_shape_score: float

    fitness: float
    error_message: str | None
