from __future__ import annotations

import random
from dataclasses import dataclass


TEAM_PRIMARY_ROLES = ("Tank", "Damage", "Healer")
ATTACKER_TEAM_ID = 1
DEFENDER_TEAM_ID = 2
DEFAULT_TILE_DISTRIBUTION = {
    "Plain": 0.8,
    "Mountain": 0.12,
    "Water": 0.08,
}


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
    if config.generated_scenarios_per_run <= 0:
        raise ValueError("Scenario generation generated_scenarios_per_run must be positive.")
    if config.map_width <= 0 or config.map_height <= 0:
        raise ValueError("Scenario generation map dimensions must be positive.")

    unit_roles_by_id = get_unit_roles_by_id(unit_templates)
    unit_secondary_roles_by_id = get_unit_secondary_roles_by_id(unit_templates)
    unit_ids_by_role = get_unit_ids_by_role(unit_templates)
    validate_team_role_coverage(unit_ids_by_role)
    rng = random.Random(config.seed)
    role_cycler = RoleUnitCycler(unit_ids_by_role, rng)
    generated_scenarios: list[dict] = []

    for scenario_index in range(config.generated_scenarios_per_run):
        generated_scenarios.append(
            create_scenario(
                unit_roles_by_id,
                unit_secondary_roles_by_id,
                role_cycler,
                config,
                scenario_index,
                rng,
            )
        )

    return generated_scenarios


class RoleUnitCycler:
    def __init__(self, unit_ids_by_role: dict[str, list[str]], rng: random.Random):
        self._unit_ids_by_role = unit_ids_by_role
        self._rng = rng
        self._queues: dict[str, list[str]] = {}

    def next_unit_id(self, role: str) -> str:
        if role not in self._queues or not self._queues[role]:
            role_unit_ids = list(self._unit_ids_by_role.get(role, []))
            if not role_unit_ids:
                raise ValueError(f"Scenario generation could not find units for role {role!r}.")
            self._rng.shuffle(role_unit_ids)
            self._queues[role] = role_unit_ids

        return self._queues[role].pop()


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


def validate_team_role_coverage(unit_ids_by_role: dict[str, list[str]]) -> None:
    missing_roles = [role for role in TEAM_PRIMARY_ROLES if not unit_ids_by_role.get(role)]
    if missing_roles:
        raise ValueError(
            "Scenario generation requires at least one unit for each team role. "
            f"Missing: {', '.join(missing_roles)}."
        )


def create_scenario(
    unit_roles_by_id: dict[str, str],
    unit_secondary_roles_by_id: dict[str, str],
    role_cycler: RoleUnitCycler,
    generation_config: ScenarioGenerationConfig,
    variant_index: int,
    rng: random.Random,
) -> dict:
    width = generation_config.map_width
    height = generation_config.map_height
    attacker_unit_ids = choose_team_unit_ids(role_cycler)
    defender_unit_ids = choose_team_unit_ids(role_cycler)

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

    return {
        "id": f"generated-scenario-{variant_index + 1}",
        "mapGen": {
            "width": width,
            "height": height,
            "tileDistribution": dict(DEFAULT_TILE_DISTRIBUTION),
        },
        "attackerTeamId": ATTACKER_TEAM_ID,
        "defenderTeamId": DEFENDER_TEAM_ID,
        "teamToAct": ATTACKER_TEAM_ID,
        "attackerTurnsTaken": 0,
        "units": build_units(attacker_unit_ids, ATTACKER_TEAM_ID, attacker_positions)
        + build_units(defender_unit_ids, DEFENDER_TEAM_ID, defender_positions),
    }


def choose_team_unit_ids(role_cycler: RoleUnitCycler) -> list[str]:
    return [role_cycler.next_unit_id(role) for role in TEAM_PRIMARY_ROLES]


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
