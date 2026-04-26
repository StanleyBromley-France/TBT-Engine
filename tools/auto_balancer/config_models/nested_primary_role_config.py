from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.primary_role_balance_config import PrimaryRoleBalanceConfig
from auto_balancer.config_models.shared_config import NestedPrimaryRolesGaConfig, ScenarioConfig


@dataclass(frozen=True)
class NestedPrimaryRoleBalanceConfig:
    optimization_round_count: int
    tank: PrimaryRoleBalanceConfig
    damage: PrimaryRoleBalanceConfig
    healer: PrimaryRoleBalanceConfig


@dataclass(frozen=True)
class NestedPrimaryRoleBalancerConfig:
    scenario: ScenarioConfig
    ga: NestedPrimaryRolesGaConfig
    balance: NestedPrimaryRoleBalanceConfig
