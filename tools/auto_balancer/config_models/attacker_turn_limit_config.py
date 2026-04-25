from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import GaConfig, ScenarioConfig


@dataclass(frozen=True)
class AttackerTurnLimitBalanceConfig:
    attacker_turn_limit_min: int
    attacker_turn_limit_max: int
    initial_attacker_turn_limit: int
    attacker_win_rate_target_min: float
    attacker_win_rate_target_max: float
    turn_limit_rate_target_min: float
    turn_limit_rate_target_max: float
    attacker_win_rate_fitness_weight: float
    turn_limit_rate_fitness_weight: float


@dataclass(frozen=True)
class AttackerTurnLimitBalancerConfig:
    scenario: ScenarioConfig
    ga: GaConfig
    balance: AttackerTurnLimitBalanceConfig
