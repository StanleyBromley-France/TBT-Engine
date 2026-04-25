from __future__ import annotations

import subprocess
from pathlib import Path

from auto_balancer.eval.config import EvalCommandConfig
from auto_balancer.runtime.paths import REPO_ROOT


def execute_eval_command(config: EvalCommandConfig, turn_budget: int, output_path: Path) -> None:
    command = [str(config.cli_path), "eval"]

    if config.log_mode == "quiet":
        command.append("--quiet")
    elif config.log_mode == "verbose":
        command.append("--verbose")

    command.extend(
        [
            "--content",
            str(config.content_path),
            "--validation",
            config.validation,
            "--seed",
            str(config.seed),
            "--repeat-count",
            str(config.repeat_count),
            "--max-turns",
            str(turn_budget),
            "--output",
            str(output_path),
        ]
    )

    if config.game_state:
        command.extend(["--game-state", config.game_state])

    if config.log_mode in {"normal", "verbose"}:
        completed = subprocess.run(
            command,
            cwd=REPO_ROOT,
            text=True,
            timeout=config.timeout_seconds,
        )
    else:
        completed = subprocess.run(
            command,
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            timeout=config.timeout_seconds,
        )

    if completed.returncode != 0:
        stdout = completed.stdout if completed.stdout is not None else ""
        stderr = completed.stderr if completed.stderr is not None else ""
        raise RuntimeError(
            "Eval command failed.\n"
            f"Command: {' '.join(command)}\n"
            f"Exit code: {completed.returncode}\n"
            f"STDOUT:\n{stdout}\n"
            f"STDERR:\n{stderr}"
        )
