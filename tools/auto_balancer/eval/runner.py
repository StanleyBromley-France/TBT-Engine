from __future__ import annotations

import tempfile
from pathlib import Path

from auto_balancer.eval.config import EvalCommandConfig
from auto_balancer.eval.paths import resolve_cli_path, resolve_content_path
from auto_balancer.eval.process import execute_eval_command
from auto_balancer.eval.results import (
    EvalDetailedSummary,
    EvalRoleAlignmentSummary,
    load_json,
    parse_eval_detailed_summary,
    parse_eval_role_alignment_summary,
    parse_eval_summary,
)


def create_eval_config(
    cli_path: Path | None,
    content_path: Path | None,
    game_state: str,
    validation: str,
    seed: int,
    repeat_count: int,
    timeout_seconds: int,
    log_mode: str = "quiet",
) -> EvalCommandConfig:
    resolved_cli_path = resolve_cli_path(cli_path)
    resolved_content_path = resolve_content_path(content_path, resolved_cli_path)

    return EvalCommandConfig(
        cli_path=resolved_cli_path,
        content_path=resolved_content_path,
        game_state=game_state,
        validation=validation,
        seed=seed,
        repeat_count=repeat_count,
        timeout_seconds=timeout_seconds,
        log_mode=log_mode,
    )


def run_eval(config: EvalCommandConfig, turn_budget: int) -> tuple[int, int, int]:
    with tempfile.TemporaryDirectory(prefix="auto-balance-attacker-turn-limit-") as temp_dir:
        output_path = Path(temp_dir) / "eval-result.json"
        execute_eval_command(config, turn_budget, output_path)
        return parse_eval_summary(load_json(output_path))


def run_eval_with_repeat_count(
    config: EvalCommandConfig,
    turn_budget: int,
    repeat_count: int,
) -> tuple[int, int, int]:
    return run_eval(config.with_repeat_count(repeat_count), turn_budget)


def run_eval_detailed(
    config: EvalCommandConfig,
    turn_budget: int,
    offensive_ability_ids: set[str],
) -> EvalDetailedSummary:
    with tempfile.TemporaryDirectory(prefix="auto-balance-terrain-distribution-") as temp_dir:
        output_path = Path(temp_dir) / "eval-result.json"
        execute_eval_command(config, turn_budget, output_path)
        return parse_eval_detailed_summary(load_json(output_path), offensive_ability_ids)


def run_eval_role_alignment(
    config: EvalCommandConfig,
    turn_budget: int,
    offensive_ability_ids: set[str],
) -> EvalRoleAlignmentSummary:
    with tempfile.TemporaryDirectory(prefix="auto-balance-role-alignment-") as temp_dir:
        output_path = Path(temp_dir) / "eval-result.json"
        execute_eval_command(config, turn_budget, output_path)
        return parse_eval_role_alignment_summary(load_json(output_path), offensive_ability_ids)
