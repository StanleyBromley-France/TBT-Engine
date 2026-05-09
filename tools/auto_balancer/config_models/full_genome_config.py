from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import ScenarioConfig
from auto_balancer.eval.staged import RepeatStage


@dataclass(frozen=True)
class FullGenomeGaConfig:
    ga_random_seed: int
    candidate_population_size: int
    generation_count: int
    mutation_probability: float
    crossover_probability: float
    evaluation_turn_budget: int
    evaluation_repeat_stages: tuple[RepeatStage, ...]
    evaluation_timeout_seconds: int
    evaluation_log_mode: str
    mcts_iteration_budget: int


@dataclass(frozen=True)
class UnitStatProfileModifiersGenomeConfig:
    gene_order: tuple[str, ...]
    profile_order: tuple[str, ...]


@dataclass(frozen=True)
class AbilityEffectGroupsGenomeConfig:
    group_order: tuple[str, ...]


@dataclass(frozen=True)
class FullGenomeGenomeConfig:
    model: str
    unit_stat_profile_modifiers: UnitStatProfileModifiersGenomeConfig
    ability_effect_groups: AbilityEffectGroupsGenomeConfig


@dataclass(frozen=True)
class UnitStatModifierBounds:
    max_hp_multiplier_percent: tuple[int, int]
    max_mana_points_multiplier_percent: tuple[int, int]
    move_points_additive_delta: tuple[int, int]
    physical_damage_received_additive_delta: tuple[int, int]
    magic_damage_received_additive_delta: tuple[int, int]


@dataclass(frozen=True)
class UnitStatModifierBoundsByPrimaryRole:
    Tank: UnitStatModifierBounds
    Healer: UnitStatModifierBounds
    Damage: UnitStatModifierBounds


@dataclass(frozen=True)
class UnitStatModifierFloors:
    move_points: int
    physical_damage_received_percent: int
    magic_damage_received_percent: int


@dataclass(frozen=True)
class UnitStatModifierCeilings:
    physical_damage_received_percent: int
    magic_damage_received_percent: int


@dataclass(frozen=True)
class UnitStatProfileModifiersSearchSpaceConfig:
    mode: str
    bounds_by_primary_role: UnitStatModifierBoundsByPrimaryRole
    absolute_floors: UnitStatModifierFloors
    absolute_ceilings: UnitStatModifierCeilings


@dataclass(frozen=True)
class AbilityEffectGroupFloors:
    flat_modifier_abs: int


@dataclass(frozen=True)
class AbilityEffectGroupsSearchSpaceConfig:
    mode: str
    component_value_multiplier_percent: tuple[int, int]
    modifier_value_multiplier_percent: tuple[int, int]
    mana_cost_multiplier_percent: tuple[int, int]
    ranged_range_additive_delta: tuple[int, int]
    range_floor: int
    range_ceiling: int
    floors: AbilityEffectGroupFloors


@dataclass(frozen=True)
class FullGenomeSearchSpaceConfig:
    unit_stat_profile_modifiers: UnitStatProfileModifiersSearchSpaceConfig
    ability_effect_groups: AbilityEffectGroupsSearchSpaceConfig


@dataclass(frozen=True)
class MatchFlowTargets:
    attacker_win_rate: tuple[float, float]
    turn_limit_rate: tuple[float, float]
    average_attacker_turn_count: tuple[float, float]


@dataclass(frozen=True)
class PrimaryRoleIdentityTargets:
    tank_survival_rate: tuple[float, float]
    tank_damage_taken_to_non_tank_ratio: tuple[float, float]
    tank_damage_dealt_to_damage_ratio: tuple[float, float]
    healer_healing_to_average_damage_taken_ratio: tuple[float, float]
    damage_damage_dealt_to_non_damage_ratio: tuple[float, float]


@dataclass(frozen=True)
class SecondaryRoleIdentityTargets:
    buffer_average_buff_uptime: tuple[float, float]
    debuffer_average_debuff_uptime: tuple[float, float]
    all_units_average_tiles_moved_total: tuple[float, float]
    non_acrobat_average_tiles_moved_total: tuple[float, float]
    acrobat_average_tiles_moved_total: tuple[float, float]
    acrobat_to_non_acrobat_move_ratio: tuple[float, float]


@dataclass(frozen=True)
class RoleProfileFairnessTargets:
    role_combination_win_rate: tuple[float, float]
    primary_family_win_rate_spread_max: float
    secondary_family_win_rate_spread_max: float


@dataclass(frozen=True)
class ChangeShapeTargets:
    ability_pct_change_std_dev: tuple[float, float]
    unit_stat_profile_spread_target: tuple[float, float]


@dataclass(frozen=True)
class FullGenomeTargetsConfig:
    match_flow: MatchFlowTargets
    primary_role_identity: PrimaryRoleIdentityTargets
    secondary_role_identity: SecondaryRoleIdentityTargets
    role_profile_fairness: RoleProfileFairnessTargets
    change_shape: ChangeShapeTargets


@dataclass(frozen=True)
class FullGenomeFitnessWeightsConfig:
    match_flow: float
    primary_role_identity: float
    secondary_role_identity: float
    role_profile_fairness: float
    change_shape: float


@dataclass(frozen=True)
class FullGenomeMutationConfig:
    unit_profile_modifier_gene_probability: float
    ability_group_gene_probability: float
    small_step_probability: float
    random_reset_probability: float
    profile_block_mutation_probability: float
    stat_step_divisor: int
    ability_step_divisor: int


@dataclass(frozen=True)
class FullGenomeBalanceConfig:
    genome: FullGenomeGenomeConfig
    search_space: FullGenomeSearchSpaceConfig
    targets: FullGenomeTargetsConfig
    fitness_weights: FullGenomeFitnessWeightsConfig
    mutation: FullGenomeMutationConfig


@dataclass(frozen=True)
class FullGenomeBalancerConfig:
    scenario: ScenarioConfig
    ga: FullGenomeGaConfig
    balance: FullGenomeBalanceConfig
