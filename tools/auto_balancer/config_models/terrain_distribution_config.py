from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import GaConfig, ScenarioConfig


@dataclass(frozen=True)
class TerrainDistributionBalanceConfig:
    plain_tile_percent_min: int
    mountain_tile_percent_min: int
    mountain_tile_percent_max: int
    water_tile_percent_min: int
    water_tile_percent_max: int
    initial_mountain_tile_percent: int
    initial_water_tile_percent: int
    turn_limit_rate_target_min: float
    turn_limit_rate_target_max: float
    average_attacker_turn_count_target_min: float
    average_attacker_turn_count_target_max: float
    average_action_count_target_min: float
    average_action_count_target_max: float
    move_action_rate_target_min: float
    move_action_rate_target_max: float
    skip_action_rate_target_min: float
    skip_action_rate_target_max: float
    offensive_ability_use_rate_target_min: float
    offensive_ability_use_rate_target_max: float
    attacker_win_rate_target_min: float
    attacker_win_rate_target_max: float
    turn_limit_rate_fitness_weight: float
    average_attacker_turn_count_fitness_weight: float
    average_action_count_fitness_weight: float
    move_action_rate_fitness_weight: float
    skip_action_rate_fitness_weight: float
    offensive_ability_use_rate_fitness_weight: float
    attacker_win_rate_fitness_weight: float


@dataclass(frozen=True)
class TerrainDistributionBalancerConfig:
    scenario: ScenarioConfig
    ga: GaConfig
    balance: TerrainDistributionBalanceConfig
