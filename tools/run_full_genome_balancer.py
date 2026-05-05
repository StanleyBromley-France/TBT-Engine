#!/usr/bin/env python3
"""Run the full-genome balancer as a standalone tool."""
from __future__ import annotations

import argparse
from datetime import datetime
from pathlib import Path
from types import SimpleNamespace

import auto_balancer.config_models as config_models
import auto_balancer.package as balance_package
import auto_balancer.runtime as runtime
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.workflows import full_genome
import run_full_genome_seed_matrix as seed_matrix_runner


TOOLS_DIR = Path(__file__).resolve().parent
DEFAULT_GA_CONFIG = TOOLS_DIR / "configs" / "ga" / "full-genome.json"
DEFAULT_SCENARIO_CONFIG = TOOLS_DIR / "configs" / "scenario" / "full-genome-scenario-eval.json"
DEFAULT_BALANCE_CONFIG = TOOLS_DIR / "configs" / "balance" / "full-genome.json"
DEFAULT_OUTPUT_ROOT = TOOLS_DIR.parent / "scratch" / "balance-runs" / "full-genome"


def load_config(args: argparse.Namespace) -> config_models.FullGenomeBalancerConfig:
    return load_balancer_config_from_args(
        config_models.FullGenomeBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def default_output_package_path(config: config_models.FullGenomeBalancerConfig) -> Path:
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    seed_pair = f"ga-seed-{config.ga.ga_random_seed}__scenario-seed-{config.scenario.scenario_generation_random_seed}"
    return DEFAULT_OUTPUT_ROOT / seed_pair / stamp


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run the standalone full-genome balancer.")
    parser.add_argument("--ga-config", type=Path, default=None, help="Path to full-genome GA config JSON.")
    parser.add_argument(
        "--scenario-config",
        type=Path,
        action="append",
        default=None,
        help="Path to scenario/eval JSON config. Can be passed more than once.",
    )
    parser.add_argument(
        "--balance-config",
        type=Path,
        default=None,
        help="Path to full-genome balance config JSON.",
    )
    parser.add_argument("--input-package", type=Path, default=None, help="Optional balance package/content input.")
    parser.add_argument("--output-package", type=Path, default=None, help="Where to write the output balance package.")
    parser.add_argument("--persist-results", action="store_true", help="Write tuned content back to the input content directory.")
    parser.add_argument(
        "--seed-matrix",
        action="store_true",
        help="Run the balanced GA/scenario seed matrix after the main full-genome run.",
    )
    parser.add_argument(
        "--ga-seeds",
        type=seed_matrix_runner.parse_seed_list,
        default=seed_matrix_runner.parse_seed_list(seed_matrix_runner.DEFAULT_GA_SEEDS),
        help="Seed-matrix GA seeds. Must have same count as --scenario-seeds.",
    )
    parser.add_argument(
        "--scenario-seeds",
        type=seed_matrix_runner.parse_seed_list,
        default=seed_matrix_runner.parse_seed_list(seed_matrix_runner.DEFAULT_SCENARIO_SEEDS),
        help="Seed-matrix scenario-generation seeds. Must have same count as --ga-seeds.",
    )
    parser.add_argument(
        "--matrix-output-root",
        type=Path,
        default=seed_matrix_runner.DEFAULT_OUTPUT_ROOT,
        help="Seed-matrix parent directory for output packages.",
    )
    parser.add_argument(
        "--matrix-config-root",
        type=Path,
        default=seed_matrix_runner.DEFAULT_CONFIG_ROOT,
        help="Seed-matrix parent directory for generated seeded configs.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="For --seed-matrix, write seeded configs and planned paths without launching runs.",
    )

    args = parser.parse_args()
    if args.ga_config is None:
        args.ga_config = DEFAULT_GA_CONFIG
    if args.balance_config is None:
        args.balance_config = DEFAULT_BALANCE_CONFIG
    if args.scenario_config is None:
        args.scenario_config = [DEFAULT_SCENARIO_CONFIG]
    return args


def run_single(args: argparse.Namespace) -> int:
    config = load_config(args)
    input_content_path = balance_package.resolve_package_content_path(
        args.input_package,
        runtime.DEFAULT_GA_CONTENT_DIR,
    )
    output_package_path = args.output_package if args.output_package is not None else default_output_package_path(config)
    return full_genome.run(
        config,
        source_content_path=input_content_path,
        output_package_path=output_package_path,
        persist_results=args.persist_results,
    )


def build_seed_matrix_args(args: argparse.Namespace) -> argparse.Namespace:
    return SimpleNamespace(
        ga_config=seed_matrix_runner.DEFAULT_GA_CONFIG,
        scenario_config=[seed_matrix_runner.DEFAULT_SCENARIO_CONFIG],
        balance_config=args.balance_config,
        input_package=args.input_package,
        ga_seeds=args.ga_seeds,
        scenario_seeds=args.scenario_seeds,
        matrix_output_root=args.matrix_output_root,
        matrix_config_root=args.matrix_config_root,
        dry_run=args.dry_run,
    )


def main() -> int:
    try:
        args = parse_args()
        runtime.disable_windows_quick_edit()
        single_exit_code = run_single(args)
        if not args.seed_matrix:
            return single_exit_code
        if single_exit_code != 0:
            return single_exit_code
        matrix_exit_code = seed_matrix_runner.run_seed_matrix(build_seed_matrix_args(args))
        return single_exit_code or matrix_exit_code
    except KeyboardInterrupt:
        print("interrupted; stopping", flush=True)
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
