from __future__ import annotations

import json
import shutil
from pathlib import Path

from auto_balancer.scenarios.generator import ScenarioGenerationConfig, generate_scenarios


GAME_STATES_FILE_NAME = "gameStates.json"
UNIT_TEMPLATES_FILE_NAME = "unitTemplates.json"
ABILITIES_FILE_NAME = "abilities.json"
EFFECT_COMPONENT_TEMPLATES_FILE_NAME = "effectComponentTemplates.json"


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


def update_ability_mana_costs(
    content_path: Path,
    mana_cost_updates: dict[str, int],
) -> None:
    """Write updated manaCost values into abilities.json.

    Args:
        content_path: Directory containing the content files.
        mana_cost_updates: Mapping of ability_id -> new manaCost value.
    """
    abilities_path = content_path / ABILITIES_FILE_NAME
    abilities = load_json_array(abilities_path)
    for ability in abilities:
        ability_id = ability.get("id")
        if ability_id in mana_cost_updates:
            ability["manaCost"] = mana_cost_updates[ability_id]
    write_json_array(abilities_path, abilities)


def load_unit_templates(content_path: Path) -> list[dict]:
    return load_json_array(content_path / UNIT_TEMPLATES_FILE_NAME)


def update_unit_templates_for_role(
    content_path: Path,
    primary_role: str | None,
    secondary_role: str | None,
    field_values: dict[str, int],
) -> list[str]:
    unit_templates_path = content_path / UNIT_TEMPLATES_FILE_NAME
    unit_templates = load_unit_templates(content_path)
    matched_unit_ids: list[str] = []

    matched_units: list[dict] = []
    for unit_template in unit_templates:
        unit_primary_role = unit_template.get("primaryRole")
        unit_secondary_role = unit_template.get("secondaryRole")
        if primary_role is not None and unit_primary_role != primary_role:
            continue
        if secondary_role is not None and unit_secondary_role != secondary_role:
            continue

        unit_id = unit_template.get("id")
        if isinstance(unit_id, str):
            matched_unit_ids.append(unit_id)
        matched_units.append(unit_template)

    if not matched_units:
        raise ValueError(
            f"No unit templates matched role filter primaryRole={primary_role!r}, secondaryRole={secondary_role!r}."
        )

    apply_role_baseline_values(matched_units, field_values)

    write_json_array(unit_templates_path, unit_templates)
    return matched_unit_ids


def load_effect_components(content_path: Path) -> list[dict]:
    return load_json_array(content_path / EFFECT_COMPONENT_TEMPLATES_FILE_NAME)


def save_file_to_source_content(
    generated_content_path: Path,
    source_content_path: Path,
    file_name: str,
) -> None:
    """Copy an optimised file from the generated content dir back to the source.

    This makes results from one pipeline stage visible to subsequent stages:
    ``prepare_generated_content`` always copies from the source, so unless the
    source is updated the next stage starts from the original untuned values.
    """
    src = generated_content_path / file_name
    dst = source_content_path / file_name
    shutil.copy2(src, dst)


def update_effect_component_values(
    content_path: Path,
    component_updates: dict[str, dict[str, int]],
) -> None:
    """Write updated numeric field values into effectComponentTemplates.json.

    Args:
        content_path: Directory containing the content files.
        component_updates: Mapping of component_id -> {field_name: new_value}.
    """
    comps_path = content_path / EFFECT_COMPONENT_TEMPLATES_FILE_NAME
    components = load_json_array(comps_path)
    for component in components:
        comp_id = component.get("id")
        if comp_id in component_updates:
            for field_name, value in component_updates[comp_id].items():
                component[field_name] = value
    write_json_array(comps_path, components)


def apply_role_baseline_values(
    unit_templates: list[dict],
    field_values: dict[str, int],
) -> None:
    for field_name, target_mean in field_values.items():
        current_values = [int(unit_template[field_name]) for unit_template in unit_templates]
        current_mean = sum(current_values) / len(current_values)
        delta = round(target_mean - current_mean)

        for unit_template, current_value in zip(unit_templates, current_values):
            unit_template[field_name] = current_value + delta
