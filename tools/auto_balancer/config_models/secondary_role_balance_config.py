from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import GaConfig, ScenarioConfig


@dataclass(frozen=True)
class SecondaryRoleBalanceConfig:
    target_secondary_role: str
    target_primary_role: str | None
    unit_max_hp_min: int
    unit_max_hp_max: int
    unit_max_mana_points_min: int
    unit_max_mana_points_max: int
    unit_move_points_min: int
    unit_move_points_max: int
    physical_damage_received_percent_min: int
    physical_damage_received_percent_max: int
    magic_damage_received_percent_min: int
    magic_damage_received_percent_max: int
    turn_limit_rate_target_min: float
    turn_limit_rate_target_max: float
    average_attacker_turn_count_target_min: float
    average_attacker_turn_count_target_max: float
    average_action_count_target_min: float
    average_action_count_target_max: float
    turn_limit_rate_fitness_weight: float
    average_attacker_turn_count_fitness_weight: float
    average_action_count_fitness_weight: float
    secondary_role_alignment_fitness_weight: float


@dataclass(frozen=True)
class SecondaryRoleBalancerConfig:
    scenario: ScenarioConfig
    ga: GaConfig
    balance: SecondaryRoleBalanceConfig
