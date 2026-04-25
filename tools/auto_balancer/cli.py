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
