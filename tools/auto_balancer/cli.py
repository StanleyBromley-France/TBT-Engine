from __future__ import annotations

import argparse
from pathlib import Path


def add_config_arguments(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--ga-config", type=Path, required=True, help="Path to GA JSON config.")
    parser.add_argument(
        "--scenario-config",
        type=Path,
        action="append",
        required=True,
        help="Path to scenario/eval JSON config. Can be passed more than once.",
    )
    parser.add_argument("--balance-config", type=Path, required=True, help="Path to balance JSON config.")


def raise_direct_balancer_cli_error(stage_hint: str | None = None) -> int:
    stage_text = f" --stage {stage_hint}" if stage_hint else " --stage <stage>"
    raise SystemExit(
        "Direct balancer scripts are internal workers now. "
        "Run balancing through the package pipeline instead, for example:\n"
        "  .\\.venv\\Scripts\\python.exe tools\\run_balancer.py "
        "--pipeline-config tools\\configs\\pipeline\\full-pipeline.json"
        f"{stage_text} --output-package scratch\\packages\\<run-name>\n"
        "Use --input-package to continue from a previous stage package."
    )
