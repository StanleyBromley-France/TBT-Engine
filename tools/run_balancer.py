#!/usr/bin/env python3
"""Run the full balancing pipeline in sequence.

Stages
------
primary-role-baselines
    Runs the nested primary-role stat balancer
    for Tank, Healer, and Damage. Establishes broad role stat baselines before
    secondary-role combination tuning.

unit-stats
    Runs the nested-combinations stat balancer
    for all 9 primary+secondary role combinations. Tunes HP, mana, move points,
    and damage-received percentages until each combination's fitness converges.

ability-effects
    Runs the ability effects balancer across all
    effect component templates. Tunes damage, heal, percent, and flat-modifier
    values so Tanks deal moderate damage, Healers heal meaningfully, Damage
    dealers output high damage, Buffers show buff uptime, and Debuffers show
    debuff uptime.

Usage
-----
    python run_balancer.py --pipeline-config configs/pipeline/full-pipeline.json

    # Run only one stage:
    python run_balancer.py --pipeline-config configs/pipeline/full-pipeline.json --stage primary-role-baselines
    python run_balancer.py --pipeline-config configs/pipeline/full-pipeline.json --stage unit-stats
    python run_balancer.py --pipeline-config configs/pipeline/full-pipeline.json --stage ability-effects

Pipeline config format (JSON)
-----------------------------
    {
      "primary_role_baselines": {
        "ga_config":       "configs/ga/nested-primary-roles.json",
        "scenario_configs": ["configs/scenario/generated-scenario-eval.json"],
        "balance_config":  "configs/balance/primary-roles-nested.json"
      },
      "unit_stats": {
        "ga_config":       "configs/ga/nested-combinations.json",
        "scenario_configs": ["configs/scenario/generated-scenario-eval.json"],
        "balance_config":  "configs/balance/nested-combinations.json"
      },
      "ability_effects": {
        "ga_config":       "configs/ga/ability-effects.json",
        "scenario_configs": ["configs/scenario/generated-scenario-eval.json"],
        "balance_config":  "configs/balance/ability-effects.json"
      }
    }

All paths in the pipeline config are resolved relative to the directory that
contains the pipeline config file itself.
"""
from __future__ import annotations

import argparse
import json
import time
from datetime import datetime
from pathlib import Path
from types import SimpleNamespace

import auto_balancer.package as balance_package
import auto_balancer.runtime as runtime
from auto_balancer.stages import primary_role_baselines, unit_stats
from auto_balancer.workflows import ability_effects


VALID_STAGES = ("primary-role-baselines", "unit-stats", "ability-effects", "all")


def load_pipeline_config(pipeline_config_path: Path) -> dict:
    with pipeline_config_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    if not isinstance(payload, dict):
        raise ValueError(f"Pipeline config must be a JSON object: {pipeline_config_path}")
    return payload


def resolve_stage_args(stage_cfg: dict, base_dir: Path) -> SimpleNamespace:
    ga_config = base_dir / stage_cfg["ga_config"]
    scenario_configs = [base_dir / sc for sc in stage_cfg["scenario_configs"]]
    balance_config = base_dir / stage_cfg["balance_config"]

    for path in [ga_config, balance_config] + scenario_configs:
        if not path.exists():
            raise FileNotFoundError(f"Pipeline config references missing file: {path}")

    return SimpleNamespace(
        ga_config=ga_config,
        scenario_config=scenario_configs,
        balance_config=balance_config,
    )


def run_stage_primary_role_baselines(
    pipeline_cfg: dict,
    base_dir: Path,
    source_content_path: Path,
    output_package_path: Path,
) -> Path:
    print("=" * 60, flush=True)
    print("STAGE: primary-role-baselines", flush=True)
    print("=" * 60, flush=True)
    t0 = time.monotonic()

    stage_cfg = pipeline_cfg.get("primary_role_baselines")
    if stage_cfg is None:
        raise ValueError("Pipeline config missing 'primary_role_baselines' section.")

    args = resolve_stage_args(stage_cfg, base_dir)
    nested_config = primary_role_baselines.load_balancer_config(args)
    exit_code = primary_role_baselines.run(
        nested_config,
        source_content_path=source_content_path,
        output_package_path=output_package_path,
        persist_results=False,
    )
    if exit_code != 0:
        raise RuntimeError(f"primary-role-baselines stage exited with code {exit_code}.")

    elapsed = time.monotonic() - t0
    print(f"primary-role-baselines stage complete ({elapsed:.0f}s)", flush=True)
    return output_package_path


def run_stage_unit_stats(
    pipeline_cfg: dict,
    base_dir: Path,
    source_content_path: Path,
    output_package_path: Path,
) -> Path:
    print("=" * 60, flush=True)
    print("STAGE: unit-stats", flush=True)
    print("=" * 60, flush=True)
    t0 = time.monotonic()

    stage_cfg = pipeline_cfg.get("unit_stats")
    if stage_cfg is None:
        raise ValueError("Pipeline config missing 'unit_stats' section.")

    args = resolve_stage_args(stage_cfg, base_dir)
    nested_config = unit_stats.load_balancer_config(args)
    exit_code = unit_stats.run(
        nested_config,
        source_content_path=source_content_path,
        output_package_path=output_package_path,
        persist_results=False,
    )
    if exit_code != 0:
        raise RuntimeError(f"unit-stats stage exited with code {exit_code}.")

    elapsed = time.monotonic() - t0
    print(f"unit-stats stage complete ({elapsed:.0f}s)", flush=True)
    return output_package_path


def run_stage_ability_effects(
    pipeline_cfg: dict,
    base_dir: Path,
    source_content_path: Path,
    output_package_path: Path,
) -> Path:
    print("=" * 60, flush=True)
    print("STAGE: ability-effects", flush=True)
    print("=" * 60, flush=True)
    t0 = time.monotonic()

    stage_cfg = pipeline_cfg.get("ability_effects")
    if stage_cfg is None:
        raise ValueError("Pipeline config missing 'ability_effects' section.")

    args = resolve_stage_args(stage_cfg, base_dir)
    ability_config = ability_effects.load_balancer_config(args)
    exit_code = ability_effects.run(
        ability_config,
        source_content_path=source_content_path,
        output_package_path=output_package_path,
        persist_results=False,
    )
    if exit_code != 0:
        raise RuntimeError(f"ability-effects stage exited with code {exit_code}.")

    elapsed = time.monotonic() - t0
    print(f"ability-effects stage complete ({elapsed:.0f}s)", flush=True)
    return output_package_path


def default_output_package_path(stage: str) -> Path:
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    return runtime.DEFAULT_GA_CONTENT_DIR.parent / "balance-packages" / f"{stage}-{stamp}"


def resolve_stage_output_path(stage: str, requested_output: Path | None, stage_name: str) -> Path:
    if requested_output is None:
        return default_output_package_path(stage_name)
    if stage == "all":
        return requested_output / stage_name
    return requested_output


def run(
    pipeline_config_path: Path,
    stage: str,
    input_package_path: Path | None,
    output_package_path: Path | None,
) -> int:
    runtime.ensure_deap_available()
    pipeline_cfg = load_pipeline_config(pipeline_config_path)
    base_dir = pipeline_config_path.parent
    current_content_path = balance_package.resolve_package_content_path(
        input_package_path,
        runtime.DEFAULT_GA_CONTENT_DIR,
    )

    t_total = time.monotonic()
    if stage in ("primary-role-baselines", "all"):
        primary_package_path = resolve_stage_output_path(stage, output_package_path, "primary-role-baselines")
        run_stage_primary_role_baselines(pipeline_cfg, base_dir, current_content_path, primary_package_path)
        current_content_path = balance_package.resolve_package_content_path(
            primary_package_path,
            runtime.DEFAULT_GA_CONTENT_DIR,
        )
    if stage in ("unit-stats", "all"):
        unit_package_path = resolve_stage_output_path(stage, output_package_path, "unit-stats")
        run_stage_unit_stats(pipeline_cfg, base_dir, current_content_path, unit_package_path)
        current_content_path = balance_package.resolve_package_content_path(
            unit_package_path,
            runtime.DEFAULT_GA_CONTENT_DIR,
        )
    if stage in ("ability-effects", "all"):
        ability_package_path = resolve_stage_output_path(stage, output_package_path, "ability-effects")
        run_stage_ability_effects(pipeline_cfg, base_dir, current_content_path, ability_package_path)
        current_content_path = balance_package.resolve_package_content_path(
            ability_package_path,
            runtime.DEFAULT_GA_CONTENT_DIR,
        )

    elapsed_total = time.monotonic() - t_total
    print(f"pipeline complete (stage={stage}, total={elapsed_total:.0f}s)", flush=True)
    print(f"final-content={current_content_path}", flush=True)
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run the full balancing pipeline."
    )
    parser.add_argument(
        "--pipeline-config",
        type=Path,
        required=True,
        help="Path to pipeline config JSON (references per-stage GA/balance/scenario configs).",
    )
    parser.add_argument(
        "--stage",
        choices=VALID_STAGES,
        default="all",
        help="Which stage(s) to run. Default: all.",
    )
    parser.add_argument(
        "--input-package",
        type=Path,
        default=None,
        help="Optional balance package to use as this run's input content.",
    )
    parser.add_argument(
        "--output-package",
        type=Path,
        default=None,
        help=(
            "Where to write the output package. For --stage all this is treated "
            "as a parent directory containing one package per stage."
        ),
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    return run(args.pipeline_config, args.stage, args.input_package, args.output_package)


if __name__ == "__main__":
    raise SystemExit(main())
