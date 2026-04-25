from __future__ import annotations

import json
from dataclasses import fields, is_dataclass
from pathlib import Path
from typing import Any, TypeVar, get_origin, get_type_hints

from auto_balancer.eval.staged import RepeatStage


ConfigT = TypeVar("ConfigT")


def load_config_file(config_path: Path) -> dict[str, Any]:
    if config_path is None:
        raise ValueError("A JSON config path is required.")

    with config_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    if not isinstance(payload, dict):
        raise ValueError(f"Config file must contain a JSON object: {config_path}")

    return payload


def load_config_files(config_paths: list[Path]) -> dict[str, Any]:
    if not config_paths:
        raise ValueError("At least one scenario JSON config path is required.")

    values: dict[str, Any] = {}
    for config_path in config_paths:
        values.update(load_config_file(config_path))

    return values


def load_ga_run_config(
    ga_config_path: Path,
    scenario_config_paths: list[Path],
    balance_config_path: Path,
) -> dict[str, Any]:
    ga_payload = load_config_file(ga_config_path)
    return {
        "scenario": load_config_files(scenario_config_paths),
        "ga": ga_payload,
        "balance": load_config_file(balance_config_path),
    }


def load_balancer_config_from_args(
    config_type: type[ConfigT],
    args: Any,
    repeat_stage_fields: tuple[str, ...] = (),
) -> ConfigT:
    payload = load_ga_run_config(args.ga_config, args.scenario_config, args.balance_config)
    payload = normalize_repeat_stage_fields(payload, repeat_stage_fields)
    return instantiate_config_from_sections(config_type, payload, ("scenario", "ga", "balance"))


def normalize_repeat_stage_fields(
    payload: dict[str, Any],
    field_names: tuple[str, ...],
) -> dict[str, Any]:
    if not field_names:
        return payload

    ga_section = payload.get("ga")
    if not isinstance(ga_section, dict):
        return payload

    normalized_payload = dict(payload)
    normalized_ga = dict(ga_section)
    for field_name in field_names:
        if field_name in normalized_ga:
            normalized_ga[field_name] = parse_repeat_stages(normalized_ga[field_name], field_name)

    normalized_payload["ga"] = normalized_ga
    return normalized_payload


def parse_repeat_stages(raw_stages: object, field_name: str) -> tuple[RepeatStage, ...]:
    if not isinstance(raw_stages, list):
        raise ValueError(f"Config field 'ga.{field_name}' must contain an array.")

    return tuple(parse_repeat_stage(stage, field_name) for stage in raw_stages)


def parse_repeat_stage(stage: object, field_name: str) -> RepeatStage:
    if not isinstance(stage, dict):
        raise ValueError(f"Each stage in ga.{field_name} must be an object with total_repeats.")

    return RepeatStage(total_repeats=int(stage["total_repeats"]))


def instantiate_config_from_sections(
    config_type: type[ConfigT],
    payload: dict[str, Any],
    section_names: tuple[str, ...],
) -> ConfigT:
    values: dict[str, Any] = {}
    section_types = get_type_hints(config_type)
    config_fields = get_dataclass_field_names(config_type)

    unknown_sections = sorted(set(payload) - set(section_names))
    if unknown_sections:
        raise ValueError("Config contained unknown sections: " + ", ".join(unknown_sections))

    for section_name in section_names:
        if section_name not in config_fields:
            raise ValueError(f"Config type {config_type.__name__} has no '{section_name}' section.")

        section_type = section_types[section_name]
        section_values = payload.get(section_name, {})
        if not isinstance(section_values, dict):
            raise ValueError(f"Config section '{section_name}' must contain an object.")

        values[section_name] = instantiate_dataclass(section_type, section_values, section_name)

    missing_fields = sorted(config_fields - set(values))
    if missing_fields:
        raise ValueError(
            f"Config for {config_type.__name__} is missing required fields: "
            + ", ".join(missing_fields)
        )
    return config_type(**values)


def instantiate_dataclass(config_type: type[ConfigT], values: dict[str, Any], section_name: str) -> ConfigT:
    allowed_fields = get_dataclass_field_names(config_type)
    unknown_fields = sorted(set(values) - allowed_fields)
    if unknown_fields:
        raise ValueError(
            f"Config section '{section_name}' contains unknown fields: "
            + ", ".join(unknown_fields)
        )

    missing_fields = sorted(allowed_fields - set(values))
    if missing_fields:
        raise ValueError(
            f"Config section '{section_name}' is missing required fields: "
            + ", ".join(missing_fields)
        )

    normalized_values = normalize_tuple_fields(config_type, values)
    return config_type(**normalized_values)


def normalize_tuple_fields(config_type: type[ConfigT], values: dict[str, Any]) -> dict[str, Any]:
    normalized_values = dict(values)
    field_types = get_type_hints(config_type)
    for field_name, field_type in field_types.items():
        raw_value = normalized_values.get(field_name)
        if is_dataclass(field_type) and isinstance(raw_value, dict):
            normalized_values[field_name] = instantiate_dataclass(field_type, raw_value, field_name)
            continue
        if is_tuple_type(field_type) and isinstance(raw_value, list):
            normalized_values[field_name] = tuple(normalized_values[field_name])
    return normalized_values


def is_tuple_type(field_type: object) -> bool:
    return get_origin(field_type) is tuple


def get_dataclass_field_names(config_type: object) -> set[str]:
    if not is_dataclass(config_type):
        raise TypeError("Expected a dataclass config type.")

    return {field_info.name for field_info in fields(config_type)}
