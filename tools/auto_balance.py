#!/usr/bin/env python3
"""Top-level entry point for auto-balancing workflows."""
from __future__ import annotations

import argparse
from types import SimpleNamespace
from types import ModuleType

from auto_balancer.cli import add_config_arguments
import auto_balance_attacker_turn_limit
import auto_balance_primary_roles
import auto_balance_primary_roles_nested
import auto_balance_terrain_distribution


BALANCERS: dict[str, ModuleType] = {
    "attacker-turn-limit": auto_balance_attacker_turn_limit,
    "primary-role": auto_balance_primary_roles,
    "primary-roles-nested": auto_balance_primary_roles_nested,
    "terrain-distribution": auto_balance_terrain_distribution,
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run an auto-balancer.")
    parser.add_argument(
        "balancer",
        choices=sorted(BALANCERS),
        help="Which balancing workflow to run.",
    )
    add_config_arguments(parser)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    balancer_args = SimpleNamespace(
        ga_config=args.ga_config,
        scenario_config=args.scenario_config,
        balance_config=args.balance_config,
    )
    balancer = BALANCERS[args.balancer]
    return balancer.run(balancer.load_balancer_config(balancer_args))


if __name__ == "__main__":
    raise SystemExit(main())
