from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def get_scenarios(payload: dict) -> list[dict]:
    scenarios = payload.get("scenarios")
    if isinstance(scenarios, list):
        return scenarios

    legacy = payload.get("Scenarios")
    if isinstance(legacy, list):
        return legacy

    raise ValueError("Eval JSON did not contain a top-level 'scenarios' array.")


@dataclass(frozen=True)
class EvalDetailedSummary:
    attacker_wins: int
    turn_limit_count: int
    total_runs: int
    average_attacker_turn_count: float
    average_action_count: float
    move_action_rate: float
    skip_action_rate: float
    ability_use_rate: float
    offensive_ability_use_rate: float
    support_ability_use_rate: float


@dataclass(frozen=True)
class EvalUnitTemplateAggregate:
    unit_template_id: str
    primary_role: str
    secondary_role: str | None
    appearances: int
    survival_rate: float
    average_damage_dealt: float
    average_damage_taken: float
    average_healing_done: float
    average_tiles_moved_total: float
    average_turns_survived: float
    average_buff_uptime_granted: float
    average_debuff_uptime_granted: float
    average_buff_effects_applied: float
    average_debuff_effects_applied: float


@dataclass(frozen=True)
class EvalRoleCombinationWinRateAggregate:
    role_combination: str
    primary_role: str
    secondary_role: str | None
    team_observations: int
    wins: int
    win_rate: float


@dataclass(frozen=True)
class EvalRoleAlignmentSummary:
    detailed: EvalDetailedSummary
    units_by_template_id: dict[str, EvalUnitTemplateAggregate]
    role_combination_win_rates: dict[str, EvalRoleCombinationWinRateAggregate]


def parse_eval_summary(payload: dict) -> tuple[int, int, int]:
    scenarios = get_scenarios(payload)
    if not scenarios:
        raise ValueError("Eval JSON did not contain any scenario results.")

    attacker_wins = 0
    turn_limit_count = 0
    for scenario in scenarios:
        result = scenario.get("result") or scenario.get("Result") or {}
        match = result.get("match") or result.get("Match") or {}
        outcome = match.get("outcome") or match.get("Outcome")
        termination_reason = match.get("terminationReason") or match.get("TerminationReason")
        if outcome == "attacker":
            attacker_wins += 1
        if termination_reason == "turn_limit":
            turn_limit_count += 1

    return attacker_wins, turn_limit_count, len(scenarios)


def parse_eval_detailed_summary(payload: dict, offensive_ability_ids: set[str]) -> EvalDetailedSummary:
    scenarios = get_scenarios(payload)
    if not scenarios:
        raise ValueError("Eval JSON did not contain any scenario results.")

    attacker_wins = 0
    turn_limit_count = 0
    total_attacker_turns = 0
    total_action_count = 0
    move_count = 0
    skip_count = 0
    ability_count = 0
    offensive_ability_count = 0
    support_ability_count = 0

    for scenario in scenarios:
        result = scenario.get("result") or scenario.get("Result") or {}
        match = result.get("match") or result.get("Match") or {}
        actions = result.get("actions") or result.get("Actions") or []

        outcome = match.get("outcome") or match.get("Outcome")
        termination_reason = match.get("terminationReason") or match.get("TerminationReason")
        attacker_turns_taken = int(match.get("attackerTurnsTaken") or match.get("AttackerTurnsTaken") or 0)
        action_count = int(match.get("actionCount") or match.get("ActionCount") or len(actions))

        if outcome == "attacker":
            attacker_wins += 1
        if termination_reason == "turn_limit":
            turn_limit_count += 1

        total_attacker_turns += attacker_turns_taken
        total_action_count += action_count

        for action in actions:
            action_type = action.get("actionType") or action.get("ActionType")
            if action_type == "MoveAction":
                move_count += 1
                continue
            if action_type == "SkipActiveUnitAction":
                skip_count += 1
                continue
            if action_type != "UseAbilityAction":
                continue

            ability_count += 1
            ability_id = action.get("abilityId") or action.get("AbilityId")
            if isinstance(ability_id, str) and ability_id in offensive_ability_ids:
                offensive_ability_count += 1
            else:
                support_ability_count += 1

    total_runs = len(scenarios)
    total_tracked_actions = move_count + skip_count + ability_count

    return EvalDetailedSummary(
        attacker_wins=attacker_wins,
        turn_limit_count=turn_limit_count,
        total_runs=total_runs,
        average_attacker_turn_count=(total_attacker_turns / total_runs),
        average_action_count=(total_action_count / total_runs),
        move_action_rate=_safe_divide(move_count, total_tracked_actions),
        skip_action_rate=_safe_divide(skip_count, total_tracked_actions),
        ability_use_rate=_safe_divide(ability_count, total_tracked_actions),
        offensive_ability_use_rate=_safe_divide(offensive_ability_count, total_tracked_actions),
        support_ability_use_rate=_safe_divide(support_ability_count, total_tracked_actions),
    )


def parse_eval_role_alignment_summary(
    payload: dict,
    offensive_ability_ids: set[str],
) -> EvalRoleAlignmentSummary:
    detailed = parse_eval_detailed_summary(payload, offensive_ability_ids)
    scenarios = get_scenarios(payload)
    aggregates: dict[str, dict[str, object]] = {}
    role_combination_win_rates: dict[str, dict[str, object]] = {}

    for scenario in scenarios:
        result = scenario.get("result") or scenario.get("Result") or {}
        match = result.get("match") or result.get("Match") or {}
        units = result.get("units") or result.get("Units") or []
        outcome = str(match.get("outcome") or match.get("Outcome") or "")
        team_combinations: dict[int, dict[str, object]] = {}

        for unit in units:
            unit_template_id = unit.get("unitTemplateId") or unit.get("UnitTemplateId")
            if not isinstance(unit_template_id, str) or not unit_template_id:
                continue

            roles = unit.get("roles") or unit.get("Roles") or {}
            final_state = unit.get("finalState") or unit.get("FinalState") or {}
            performance = unit.get("performance") or unit.get("Performance") or {}
            team_id = int(unit.get("teamId") or unit.get("TeamId") or 0)
            side = str(unit.get("side") or unit.get("Side") or "").lower()
            primary_role = roles.get("primaryRole") or roles.get("PrimaryRole") or "Unknown"
            secondary_role = roles.get("secondaryRole") or roles.get("SecondaryRole")

            aggregate = aggregates.setdefault(
                unit_template_id,
                {
                    "unit_template_id": unit_template_id,
                    "primary_role": primary_role,
                    "secondary_role": secondary_role,
                    "appearances": 0,
                    "alive_count": 0,
                    "damage_dealt": 0,
                    "damage_taken": 0,
                    "healing_done": 0,
                    "tiles_moved_total": 0,
                    "turns_survived": 0,
                    "buff_uptime_granted": 0,
                    "debuff_uptime_granted": 0,
                    "buff_effects_applied": 0,
                    "debuff_effects_applied": 0,
                },
            )

            aggregate["appearances"] = int(aggregate["appearances"]) + 1
            if final_state.get("alive") or final_state.get("Alive"):
                aggregate["alive_count"] = int(aggregate["alive_count"]) + 1
            aggregate["damage_dealt"] = int(aggregate["damage_dealt"]) + int(
                performance.get("damageDealt") or performance.get("DamageDealt") or 0
            )
            aggregate["damage_taken"] = int(aggregate["damage_taken"]) + int(
                performance.get("damageTaken") or performance.get("DamageTaken") or 0
            )
            aggregate["healing_done"] = int(aggregate["healing_done"]) + int(
                performance.get("healingDone") or performance.get("HealingDone") or 0
            )
            aggregate["tiles_moved_total"] = int(aggregate["tiles_moved_total"]) + int(
                performance.get("tilesMovedTotal") or performance.get("TilesMovedTotal") or 0
            )
            aggregate["turns_survived"] = int(aggregate["turns_survived"]) + int(
                performance.get("turnsSurvived") or performance.get("TurnsSurvived") or 0
            )
            aggregate["buff_uptime_granted"] = int(aggregate["buff_uptime_granted"]) + int(
                performance.get("buffUptimeTicksGranted") or performance.get("BuffUptimeTicksGranted") or 0
            )
            aggregate["debuff_uptime_granted"] = int(aggregate["debuff_uptime_granted"]) + int(
                performance.get("debuffUptimeTicksGranted") or performance.get("DebuffUptimeTicksGranted") or 0
            )
            aggregate["buff_effects_applied"] = int(aggregate["buff_effects_applied"]) + int(
                performance.get("buffEffectsApplied") or performance.get("BuffEffectsApplied") or 0
            )
            aggregate["debuff_effects_applied"] = int(aggregate["debuff_effects_applied"]) + int(
                performance.get("debuffEffectsApplied") or performance.get("DebuffEffectsApplied") or 0
            )

            if team_id != 0 and isinstance(primary_role, str):
                role_combination = format_role_combination(primary_role, secondary_role)
                team_entry = team_combinations.setdefault(
                    team_id,
                    {
                        "side": side,
                        "role_combinations": set(),
                    },
                )
                team_entry["role_combinations"].add((role_combination, primary_role, secondary_role))

        for team_entry in team_combinations.values():
            side = str(team_entry["side"])
            won = (outcome == "attacker" and side == "attacker") or (outcome == "defender" and side == "defender")
            for role_combination, primary_role, secondary_role in team_entry["role_combinations"]:
                role_win_rate = role_combination_win_rates.setdefault(
                    role_combination,
                    {
                        "role_combination": role_combination,
                        "primary_role": primary_role,
                        "secondary_role": secondary_role,
                        "team_observations": 0,
                        "wins": 0,
                    },
                )
                role_win_rate["team_observations"] = int(role_win_rate["team_observations"]) + 1
                if won:
                    role_win_rate["wins"] = int(role_win_rate["wins"]) + 1

    units_by_template_id: dict[str, EvalUnitTemplateAggregate] = {}
    for unit_template_id, aggregate in aggregates.items():
        appearances = int(aggregate["appearances"])
        units_by_template_id[unit_template_id] = EvalUnitTemplateAggregate(
            unit_template_id=unit_template_id,
            primary_role=str(aggregate["primary_role"]),
            secondary_role=aggregate["secondary_role"] if isinstance(aggregate["secondary_role"], str) else None,
            appearances=appearances,
            survival_rate=_safe_divide(int(aggregate["alive_count"]), appearances),
            average_damage_dealt=_safe_divide(int(aggregate["damage_dealt"]), appearances),
            average_damage_taken=_safe_divide(int(aggregate["damage_taken"]), appearances),
            average_healing_done=_safe_divide(int(aggregate["healing_done"]), appearances),
            average_tiles_moved_total=_safe_divide(int(aggregate["tiles_moved_total"]), appearances),
            average_turns_survived=_safe_divide(int(aggregate["turns_survived"]), appearances),
            average_buff_uptime_granted=_safe_divide(int(aggregate["buff_uptime_granted"]), appearances),
            average_debuff_uptime_granted=_safe_divide(int(aggregate["debuff_uptime_granted"]), appearances),
            average_buff_effects_applied=_safe_divide(int(aggregate["buff_effects_applied"]), appearances),
            average_debuff_effects_applied=_safe_divide(int(aggregate["debuff_effects_applied"]), appearances),
        )

    win_rates_by_role_combination: dict[str, EvalRoleCombinationWinRateAggregate] = {}
    for role_combination, aggregate in role_combination_win_rates.items():
        team_observations = int(aggregate["team_observations"])
        wins = int(aggregate["wins"])
        win_rates_by_role_combination[role_combination] = EvalRoleCombinationWinRateAggregate(
            role_combination=role_combination,
            primary_role=str(aggregate["primary_role"]),
            secondary_role=aggregate["secondary_role"] if isinstance(aggregate["secondary_role"], str) else None,
            team_observations=team_observations,
            wins=wins,
            win_rate=_safe_divide(wins, team_observations),
        )

    return EvalRoleAlignmentSummary(
        detailed=detailed,
        units_by_template_id=units_by_template_id,
        role_combination_win_rates=win_rates_by_role_combination,
    )


def format_role_combination(primary_role: str, secondary_role: object) -> str:
    if isinstance(secondary_role, str) and secondary_role:
        return f"{primary_role}+{secondary_role}"
    return primary_role


def _safe_divide(numerator: int, denominator: int) -> float:
    if denominator <= 0:
        return 0.0
    return numerator / denominator
