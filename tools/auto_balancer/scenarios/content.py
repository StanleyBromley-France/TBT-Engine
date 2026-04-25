from __future__ import annotations

import json
import shutil
from pathlib import Path

from auto_balancer.scenarios.generator import ScenarioGenerationConfig, generate_scenarios


GAME_STATES_FILE_NAME = "gameStates.json"
UNIT_TEMPLATES_FILE_NAME = "unitTemplates.json"
ABILITIES_FILE_NAME = "abilities.json"


def prepare_generated_content(
    source_content_path: Path,
    generated_content_path: Path,
    config: ScenarioGenerationConfig,
) -> Path:
    if not source_content_path.is_dir():
        raise FileNotFoundError(f"Source content directory was not found: {source_content_path}")

    base_game_states = load_json_array(source_content_path / GAME_STATES_FILE_NAME)
    unit_templates = load_json_array(source_content_path / UNIT_TEMPLATES_FILE_NAME)
    generated_game_states = generate_scenarios(base_game_states, unit_templates, config)

    if generated_content_path.exists():
        shutil.rmtree(generated_content_path)

    generated_content_path.mkdir(parents=True, exist_ok=True)
    copy_base_content_files(source_content_path, generated_content_path)
    write_json_array(generated_content_path / GAME_STATES_FILE_NAME, generated_game_states)
    return generated_content_path


def load_json_array(path: Path) -> list[dict]:
    payload = load_json(path)

    if not isinstance(payload, list):
        raise ValueError(f"Expected a JSON array in {path}.")

    return payload


def load_json(path: Path) -> object:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_json_array(path: Path, payload: list[dict]) -> None:
    write_json(path, payload)


def write_json(path: Path, payload: object) -> None:
    with path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")


def copy_base_content_files(source_content_path: Path, generated_content_path: Path) -> None:
    for source_path in source_content_path.iterdir():
        if not source_path.is_file():
            continue

        destination_path = generated_content_path / source_path.name
        shutil.copy2(source_path, destination_path)


def build_generated_content_path(
    source_content_path: Path,
    scenario_generation_seed: int,
    generated_scenarios_per_run: int,
) -> Path:
    return (
        source_content_path
        / f"scenario-seed-{scenario_generation_seed}"
        / f"set-size-{generated_scenarios_per_run}"
    )


def update_tile_distribution_for_game_states(
    content_path: Path,
    tile_distribution: dict[str, float],
) -> None:
    game_states_path = content_path / GAME_STATES_FILE_NAME
    game_states = load_json_array(game_states_path)

    for game_state in game_states:
        map_gen = game_state.setdefault("mapGen", {})
        if not isinstance(map_gen, dict):
            raise ValueError(f"Expected 'mapGen' to be an object in {game_states_path}.")
        map_gen["tileDistribution"] = dict(tile_distribution)

    write_json_array(game_states_path, game_states)


def load_abilities(content_path: Path) -> list[dict]:
    return load_json_array(content_path / ABILITIES_FILE_NAME)


def load_unit_templates(content_path: Path) -> list[dict]:
    return load_json_array(content_path / UNIT_TEMPLATES_FILE_NAME)


def update_unit_templates_for_role(
    content_path: Path,
    primary_role: str,
    secondary_role: str | None,
    field_values: dict[str, int],
) -> list[str]:
    unit_templates_path = content_path / UNIT_TEMPLATES_FILE_NAME
    unit_templates = load_unit_templates(content_path)
    matched_unit_ids: list[str] = []

    for unit_template in unit_templates:
        unit_primary_role = unit_template.get("primaryRole")
        unit_secondary_role = unit_template.get("secondaryRole")
        if unit_primary_role != primary_role:
            continue
        if secondary_role is not None and unit_secondary_role != secondary_role:
            continue

        unit_id = unit_template.get("id")
        if isinstance(unit_id, str):
            matched_unit_ids.append(unit_id)

        for field_name, field_value in field_values.items():
            unit_template[field_name] = field_value

    if not matched_unit_ids:
        raise ValueError(
            f"No unit templates matched role filter primaryRole={primary_role!r}, secondaryRole={secondary_role!r}."
        )

    write_json_array(unit_templates_path, unit_templates)
    return matched_unit_ids
