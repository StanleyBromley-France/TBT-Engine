from __future__ import annotations

from pathlib import Path

from auto_balancer.scenarios.content import load_abilities


def load_offensive_ability_ids(content_path: Path) -> set[str]:
    offensive_ability_ids: set[str] = set()
    for ability in load_abilities(content_path):
        ability_id = ability.get("id")
        targeting = ability.get("targeting") or {}
        allowed_target = targeting.get("allowedTarget")
        if isinstance(ability_id, str) and allowed_target == "Enemy":
            offensive_ability_ids.add(ability_id)

    if not offensive_ability_ids:
        raise ValueError(f"Could not resolve any offensive abilities from {content_path}.")

    return offensive_ability_ids
