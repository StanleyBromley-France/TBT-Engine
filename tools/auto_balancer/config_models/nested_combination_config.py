from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.secondary_role_balance_config import SecondaryRoleBalanceConfig
from auto_balancer.config_models.shared_config import NestedCombinationsGaConfig, ScenarioConfig


@dataclass(frozen=True)
class NestedCombinationBalanceConfig:
    optimization_round_count: int
    tank_buffer: SecondaryRoleBalanceConfig
    tank_debuffer: SecondaryRoleBalanceConfig
    tank_acrobat: SecondaryRoleBalanceConfig
    healer_buffer: SecondaryRoleBalanceConfig
    healer_debuffer: SecondaryRoleBalanceConfig
    healer_acrobat: SecondaryRoleBalanceConfig
    damage_buffer: SecondaryRoleBalanceConfig
    damage_debuffer: SecondaryRoleBalanceConfig
    damage_acrobat: SecondaryRoleBalanceConfig


@dataclass(frozen=True)
class NestedCombinationBalancerConfig:
    scenario: ScenarioConfig
    ga: NestedCombinationsGaConfig
    balance: NestedCombinationBalanceConfig
