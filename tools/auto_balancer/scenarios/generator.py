from __future__ import annotations

import copy
import random
from dataclasses import dataclass


@dataclass(frozen=True)
class ScenarioGenerationConfig:
    seed: int
    generated_scenarios_per_run: int
    map_width: int
    map_height: int


def generate_scenarios(
    base_game_states: list[dict],
    unit_templates: list[dict],
    config: ScenarioGenerationConfig,
) -> list[dict]:
    if not base_game_states:
        raise ValueError("At least one base game state is required for scenario generation.")
    if config.generated_scenarios_per_run <= 0:
        raise ValueError("Scenario generation generated_scenarios_per_run must be positive.")
    if config.map_width <= 0 or config.map_height <= 0:
        raise ValueError("Scenario generation map dimensions must be positive.")

    unit_template_ids = get_unit_template_ids(unit_templates)
    unit_roles_by_id = get_unit_roles_by_id(unit_templates)
    unit_secondary_roles_by_id = get_unit_secondary_roles_by_id(unit_templates)
    unit_ids_by_role = get_unit_ids_by_role(unit_templates)
    rng = random.Random(config.seed)
    generated_scenarios: list[dict] = []

    for scenario_index in range(config.generated_scenarios_per_run):
        base_game_state = rng.choice(base_game_states)
        generated_scenarios.append(
            create_variant(
                base_game_state,
                unit_template_ids,
                unit_roles_by_id,
                unit_secondary_roles_by_id,
                unit_ids_by_role,
                config,
                scenario_index,
                rng,
            )
        )

    return generated_scenarios


def get_unit_template_ids(unit_templates: list[dict]) -> list[str]:
    template_ids: list[str] = []
    for unit_template in unit_templates:
        unit_id = unit_template.get("id")
        if isinstance(unit_id, str) and unit_id:
            template_ids.append(unit_id)

    if not template_ids:
        raise ValueError("Scenario generation could not find any unit template ids.")

    return template_ids


def get_unit_roles_by_id(unit_templates: list[dict]) -> dict[str, str]:
    roles_by_id: dict[str, str] = {}
    for unit_template in unit_templates:
        unit_id = unit_template.get("id")
        primary_role = unit_template.get("primaryRole")
        if isinstance(unit_id, str) and unit_id and isinstance(primary_role, str) and primary_role:
            roles_by_id[unit_id] = primary_role

    if not roles_by_id:
        raise ValueError("Scenario generation could not resolve any unit roles.")

    return roles_by_id


def get_unit_secondary_roles_by_id(unit_templates: list[dict]) -> dict[str, str]:
    secondary_roles_by_id: dict[str, str] = {}
    for unit_template in unit_templates:
        unit_id = unit_template.get("id")
        secondary_role = unit_template.get("secondaryRole")
        if isinstance(unit_id, str) and unit_id and isinstance(secondary_role, str) and secondary_role:
            secondary_roles_by_id[unit_id] = secondary_role

    return secondary_roles_by_id


def get_unit_ids_by_role(unit_templates: list[dict]) -> dict[str, list[str]]:
    unit_ids_by_role: dict[str, list[str]] = {}
    for unit_template in unit_templates:
        unit_id = unit_template.get("id")
        primary_role = unit_template.get("primaryRole")
        if not isinstance(unit_id, str) or not unit_id:
            continue
        if not isinstance(primary_role, str) or not primary_role:
            continue

        if primary_role not in unit_ids_by_role:
            unit_ids_by_role[primary_role] = []
        unit_ids_by_role[primary_role].append(unit_id)

    if not unit_ids_by_role:
        raise ValueError("Scenario generation could not group any unit ids by role.")

    return unit_ids_by_role


def create_variant(
    base_game_state: dict,
    unit_template_ids: list[str],
    unit_roles_by_id: dict[str, str],
    unit_secondary_roles_by_id: dict[str, str],
    unit_ids_by_role: dict[str, list[str]],
    generation_config: ScenarioGenerationConfig,
    variant_index: int,
    rng: random.Random,
) -> dict:
    scenario = copy.deepcopy(base_game_state)
    scenario["id"] = f"{base_game_state['id']}-generated-{variant_index + 1}"

    attacker_team_id = int(base_game_state["attackerTeamId"])
    defender_team_id = int(base_game_state["defenderTeamId"])
    width = generation_config.map_width
    height = generation_config.map_height
    scenario["mapGen"]["width"] = width
    scenario["mapGen"]["height"] = height

    attacker_base_units = get_team_units(base_game_state, attacker_team_id)
    defender_base_units = get_team_units(base_game_state, defender_team_id)

    attacker_unit_count = len(attacker_base_units)
    defender_unit_count = len(defender_base_units)

    attacker_unit_ids = choose_role_preserving_unit_ids(
        attacker_base_units,
        unit_template_ids,
        unit_roles_by_id,
        unit_ids_by_role,
        rng,
    )
    defender_unit_ids = choose_role_preserving_unit_ids(
        defender_base_units,
        unit_template_ids,
        unit_roles_by_id,
        unit_ids_by_role,
        rng,
    )

    attacker_positions = choose_team_positions(
        width,
        height,
        team_index=0,
        unit_ids=attacker_unit_ids,
        unit_roles_by_id=unit_roles_by_id,
        unit_secondary_roles_by_id=unit_secondary_roles_by_id,
        rng=rng,
    )
    defender_positions = choose_team_positions(
        width,
        height,
        team_index=1,
        unit_ids=defender_unit_ids,
        unit_roles_by_id=unit_roles_by_id,
        unit_secondary_roles_by_id=unit_secondary_roles_by_id,
        rng=rng,
        reserved_offsets=set(attacker_positions),
    )

    scenario["units"] = build_units(attacker_unit_ids, attacker_team_id, attacker_positions) + build_units(
        defender_unit_ids,
        defender_team_id,
        defender_positions,
    )
    return scenario


def count_team_units(game_state: dict, team_id: int) -> int:
    return sum(1 for unit in game_state["units"] if int(unit["teamId"]) == team_id)


def get_team_units(game_state: dict, team_id: int) -> list[dict]:
    return [unit for unit in game_state["units"] if int(unit["teamId"]) == team_id]


def choose_unit_ids(unit_template_ids: list[str], unit_count: int, rng: random.Random) -> list[str]:
    if unit_count <= len(unit_template_ids):
        return rng.sample(unit_template_ids, unit_count)

    return [rng.choice(unit_template_ids) for _ in range(unit_count)]


def choose_role_preserving_unit_ids(
    base_units: list[dict],
    unit_template_ids: list[str],
    unit_roles_by_id: dict[str, str],
    unit_ids_by_role: dict[str, list[str]],
    rng: random.Random,
) -> list[str]:
    chosen_unit_ids: list[str] = []
    used_unit_ids: set[str] = set()

    for base_unit in base_units:
        base_unit_id = base_unit["id"]
        base_role = unit_roles_by_id.get(base_unit_id)
        role_candidates = list(unit_ids_by_role.get(base_role, []))
        available_role_candidates = [candidate for candidate in role_candidates if candidate not in used_unit_ids]

        if available_role_candidates:
            chosen_unit_id = rng.choice(available_role_candidates)
        elif role_candidates:
            chosen_unit_id = rng.choice(role_candidates)
        else:
            chosen_unit_id = choose_unit_ids(unit_template_ids, 1, rng)[0]

        chosen_unit_ids.append(chosen_unit_id)
        used_unit_ids.add(chosen_unit_id)

    return chosen_unit_ids


def choose_team_positions(
    width: int,
    height: int,
    team_index: int,
    unit_ids: list[str],
    unit_roles_by_id: dict[str, str],
    unit_secondary_roles_by_id: dict[str, str],
    rng: random.Random,
    reserved_offsets: set[tuple[int, int]] | None = None,
) -> list[tuple[int, int]]:
    reserved = set() if reserved_offsets is None else set(reserved_offsets)
    ordered_offsets = get_ordered_spawn_zone_offsets(width, height, team_index)
    available_offsets = [offset for offset in ordered_offsets if offset not in reserved]

    if len(available_offsets) < len(unit_ids):
        raise ValueError("Scenario generation could not find enough unique spawn positions.")

    tank_indices = [index for index, unit_id in enumerate(unit_ids) if unit_roles_by_id.get(unit_id) == "Tank"]
    support_indices = [
        index
        for index, unit_id in enumerate(unit_ids)
        if is_pure_support(unit_id, unit_roles_by_id, unit_secondary_roles_by_id)
    ]
    midline_indices = [
        index
        for index, unit_id in enumerate(unit_ids)
        if index not in tank_indices and index not in support_indices
    ]
    tank_count = len(tank_indices)
    support_count = len(support_indices)

    frontline_offsets = available_offsets[:tank_count]
    backline_offsets = available_offsets[len(available_offsets) - support_count :]
    midline_offsets = available_offsets[tank_count : len(available_offsets) - support_count]

    if (
        len(frontline_offsets) < tank_count
        or len(midline_offsets) < len(midline_indices)
        or len(backline_offsets) < support_count
    ):
        raise ValueError("Scenario generation could not reserve enough role-aware spawn positions.")

    chosen_offsets_by_index: dict[int, tuple[int, int]] = {}
    for unit_index, offset in zip(tank_indices, sample_offsets_by_row(frontline_offsets, tank_count, rng), strict=True):
        chosen_offsets_by_index[unit_index] = offset
    for unit_index, offset in zip(
        midline_indices,
        sample_offsets_by_row(midline_offsets, len(midline_indices), rng),
        strict=True,
    ):
        chosen_offsets_by_index[unit_index] = offset
    for unit_index, offset in zip(
        support_indices,
        sample_offsets_by_row(backline_offsets, support_count, rng),
        strict=True,
    ):
        chosen_offsets_by_index[unit_index] = offset

    return [chosen_offsets_by_index[index] for index in range(len(unit_ids))]


def is_pure_support(
    unit_id: str,
    unit_roles_by_id: dict[str, str],
    unit_secondary_roles_by_id: dict[str, str],
) -> bool:
    primary_role = unit_roles_by_id.get(unit_id)
    secondary_role = unit_secondary_roles_by_id.get(unit_id)
    if primary_role in {"Tank", "Damage"}:
        return False
    return primary_role == "Healer" or secondary_role == "Buffer"


def get_ordered_spawn_zone_offsets(width: int, height: int, team_index: int) -> list[tuple[int, int]]:
    zone_width = max(2, width // 3)
    if team_index == 0:
        columns = list(range(0, min(width, zone_width)))
        columns.sort(reverse=True)
    else:
        columns = list(range(max(0, width - zone_width), width))
        columns.sort()

    rows = ordered_rows(height)
    return [(col, row) for col in columns for row in rows]


def ordered_rows(height: int) -> list[int]:
    center = (height - 1) / 2.0
    return sorted(range(height), key=lambda row: (abs(row - center), row))


def sample_offsets_by_row(
    available_offsets: list[tuple[int, int]],
    count: int,
    rng: random.Random,
) -> list[tuple[int, int]]:
    if count <= 0:
        return []

    grouped_by_column: dict[int, list[tuple[int, int]]] = {}
    for offset in available_offsets:
        grouped_by_column.setdefault(offset[0], []).append(offset)

    chosen_offsets: list[tuple[int, int]] = []
    for column in grouped_by_column:
        rng.shuffle(grouped_by_column[column])

    for offset in available_offsets:
        column_offsets = grouped_by_column[offset[0]]
        if not column_offsets:
            continue
        candidate = column_offsets.pop()
        chosen_offsets.append(candidate)
        if len(chosen_offsets) == count:
            return chosen_offsets

    raise ValueError("Scenario generation could not sample enough spawn positions.")


def build_units(unit_ids: list[str], team_id: int, offsets: list[tuple[int, int]]) -> list[dict]:
    units: list[dict] = []
    for unit_id, (col, row) in zip(unit_ids, offsets, strict=True):
        q, r = offset_to_axial(col, row)
        units.append(
            {
                "id": unit_id,
                "teamId": team_id,
                "q": q,
                "r": r,
            }
        )

    return units


def offset_to_axial(col: int, row: int) -> tuple[int, int]:
    q = col
    r = row - (col - (col & 1)) // 2
    return q, r
