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
    primary_tank_score: float
    primary_damage_score: float
    primary_healer_score: float
    secondary_role_identity_score: float
    secondary_buffer_score: float
    secondary_debuffer_score: float
    secondary_all_units_move_score: float | None
    secondary_non_acrobat_move_score: float | None
    secondary_acrobat_move_score: float | None
    secondary_acrobat_ratio_score: float | None
    role_profile_fairness_score: float
    change_shape_score: float

    fitness: float
    error_message: str | None
