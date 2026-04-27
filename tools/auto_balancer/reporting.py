from __future__ import annotations

from collections.abc import Iterable
from typing import Any


Field = tuple[str, Any, str | None]
Metric = tuple[str, str]


def field(name: str, value: Any, format_spec: str | None = None) -> Field:
    return (name, value, format_spec)


def format_field(item: Field) -> str:
    name, value, format_spec = item
    if format_spec is None:
        rendered = str(value)
    else:
        rendered = format(value, format_spec)
    return f"{name}={rendered}"


def format_record(prefix: str, fields: Iterable[Field]) -> str:
    rendered_fields = " ".join(format_field(item) for item in fields)
    if not rendered_fields:
        return prefix
    return f"{prefix} {rendered_fields}"


def print_record(prefix: str, fields: Iterable[Field]) -> None:
    print(format_record(prefix, fields), flush=True)


def stat_fields(measurement: Any) -> list[Field]:
    return [
        field("hp", measurement.unit_max_hp),
        field("mana", measurement.unit_max_mana_points),
        field("move", measurement.unit_move_points),
        field("physDR", measurement.physical_damage_received_percent),
        field("magicDR", measurement.magic_damage_received_percent),
    ]


def primary_role_fields(measurement: Any) -> list[Field]:
    return [
        *stat_fields(measurement),
        field("turn-limit-rate", measurement.turn_limit_rate, ".2%"),
        field("avg-attacker-turns", measurement.average_attacker_turn_count, ".2f"),
        field("primary-role-score", measurement.primary_role_alignment_score, ".4f"),
        field("fitness", measurement.fitness, ".4f"),
    ]


def secondary_role_fields(measurement: Any) -> list[Field]:
    return [
        *stat_fields(measurement),
        field("turn-limit-rate", measurement.turn_limit_rate, ".2%"),
        field("avg-attacker-turns", measurement.average_attacker_turn_count, ".2f"),
        field("primary-role-score", measurement.primary_role_value_score, ".4f"),
        field("secondary-role-score", measurement.secondary_role_alignment_score, ".4f"),
        field("fitness", measurement.fitness, ".4f"),
    ]


def secondary_role_round_fields(measurement: Any) -> list[Field]:
    return [
        *stat_fields(measurement),
        field("turn-limit-rate", measurement.turn_limit_rate, ".2%"),
        field("avg-attacker-turns", measurement.average_attacker_turn_count, ".2f"),
        field("secondary-role-score", measurement.secondary_role_alignment_score, ".4f"),
        field("fitness", measurement.fitness, ".4f"),
    ]


def ability_effect_fields(measurement: Any, *, detailed: bool) -> list[Field]:
    fields = [
        field("winrate", measurement.attacker_win_rate, ".2%"),
        field("tank-dmg", measurement.average_tank_damage_dealt, ".1f"),
        field("healer-heal", measurement.average_healer_healing_done, ".1f"),
        field("damage-dmg", measurement.average_damage_damage_dealt, ".1f"),
        field("buffer-uptime", measurement.average_buffer_buff_uptime, ".2f"),
        field("debuffer-uptime", measurement.average_debuffer_debuff_uptime, ".2f"),
    ]
    if detailed:
        fields.extend(
            [
                field("pct-std", measurement.pct_change_std_dev, ".4f"),
                field("winrate-score", measurement.win_rate_score, ".4f"),
                field("primary-score", measurement.primary_role_score, ".4f"),
            ]
        )
    fields.extend(
        [
            field("tradeoff-score", measurement.role_tradeoff_score, ".4f"),
            field("dominance-score", measurement.role_dominance_score, ".4f"),
        ]
    )
    if detailed:
        fields.extend(
            [
                field("secondary-score", measurement.secondary_role_score, ".4f"),
                field("diversity-score", measurement.diversity_score, ".4f"),
            ]
        )
    fields.append(field("fitness", measurement.fitness, ".4f"))
    return fields


def build_metric_evidence(before: Any, after: Any, metrics: Iterable[Metric]) -> dict[str, float | bool]:
    evidence: dict[str, float | bool] = {}
    for label, attr_name in metrics:
        before_value = getattr(before, attr_name)
        after_value = getattr(after, attr_name)
        evidence[f"before{label}"] = before_value
        evidence[f"after{label}"] = after_value
        evidence[f"{label[0].lower()}{label[1:]}Delta"] = after_value - before_value
        evidence[f"improved{label}"] = after_value > before_value
    return evidence


def build_evidence_report(
    before_by_name: dict[str, Any],
    after_by_name: dict[str, Any],
    metrics: Iterable[Metric],
) -> dict:
    evidence: dict[str, dict[str, float | bool]] = {}
    for name, after in after_by_name.items():
        before = before_by_name.get(name)
        if before is None:
            continue
        evidence[name] = build_metric_evidence(before, after, metrics)
    return {"evidence": evidence}
