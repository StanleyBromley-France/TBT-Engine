#!/usr/bin/env python3
"""Run the full-genome seed matrix as a standalone tool."""
from __future__ import annotations

import argparse
import json
from datetime import datetime
from pathlib import Path
from typing import Any
from types import SimpleNamespace

import auto_balancer.config_models as config_models
import auto_balancer.package as balance_package
import auto_balancer.runtime as runtime
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.workflows import full_genome


TOOLS_DIR = Path(__file__).resolve().parent
DEFAULT_GA_CONFIG = TOOLS_DIR / "configs" / "ga" / "full-genome-matrix.json"
DEFAULT_SCENARIO_CONFIG = TOOLS_DIR / "configs" / "scenario" / "full-genome-matrix-scenario-eval.json"
DEFAULT_BALANCE_CONFIG = TOOLS_DIR / "configs" / "balance" / "full-genome.json"
DEFAULT_OUTPUT_ROOT = TOOLS_DIR.parent / "scratch" / "balance-runs" / "full-genome-seed-matrix"
DEFAULT_CONFIG_ROOT = TOOLS_DIR.parent / "scratch" / "balance-runs" / "full-genome-seed-matrix-configs"
DEFAULT_GA_SEEDS = "6157321,6158321,6159321"
DEFAULT_SCENARIO_SEEDS = "3422345,3423345,3424345"


def parse_seed_list(raw_value: str) -> list[int]:
    seeds = [int(item.strip()) for item in raw_value.split(",") if item.strip()]
    if not seeds:
        raise argparse.ArgumentTypeError("seed list must contain at least one integer")
    return seeds


def load_config(args: argparse.Namespace) -> config_models.FullGenomeBalancerConfig:
    return load_balancer_config_from_args(
        config_models.FullGenomeBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def build_cycle_pairs(ga_seeds: list[int], scenario_seeds: list[int]) -> list[tuple[int, int]]:
    if len(ga_seeds) != len(scenario_seeds):
        raise ValueError(
            "The cycle matrix needs the same number of GA and scenario seeds "
            "so both sides get equal representation."
        )

    pairs: list[tuple[int, int]] = []
    for index, scenario_seed in enumerate(scenario_seeds):
        pairs.append((ga_seeds[index], scenario_seed))
        pairs.append((ga_seeds[(index + 1) % len(ga_seeds)], scenario_seed))
    return pairs


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")


def seed_ga_config(source_path: Path, target_path: Path, ga_seed: int) -> None:
    payload = load_json(source_path)
    if not isinstance(payload, dict):
        raise ValueError(f"GA config must be a JSON object: {source_path}")
    payload["ga_random_seed"] = ga_seed
    write_json(target_path, payload)


def seed_scenario_config(source_path: Path, target_path: Path, scenario_seed: int) -> None:
    payload = load_json(source_path)
    if not isinstance(payload, dict):
        raise ValueError(f"Scenario config must be a JSON object: {source_path}")
    payload["scenario_generation_random_seed"] = scenario_seed
    write_json(target_path, payload)


def make_unique_run_dir(root: Path, run_index: int, ga_seed: int, scenario_seed: int) -> Path:
    base_name = f"run-{run_index:02d}__ga-seed-{ga_seed}__scenario-seed-{scenario_seed}"
    candidate = root / base_name
    suffix = 2
    while candidate.exists():
        candidate = root / f"{base_name}__{suffix}"
        suffix += 1
    return candidate


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run the standalone full-genome seed matrix.")
    parser.add_argument("--ga-config", type=Path, default=DEFAULT_GA_CONFIG, help="Path to matrix GA config JSON.")
    parser.add_argument(
        "--scenario-config",
        type=Path,
        action="append",
        default=None,
        help="Path to matrix scenario/eval JSON config. Can be passed once.",
    )
    parser.add_argument(
        "--balance-config",
        type=Path,
        default=DEFAULT_BALANCE_CONFIG,
        help="Path to full-genome balance config JSON.",
    )
    parser.add_argument("--input-package", type=Path, default=None, help="Optional balance package/content input.")
    parser.add_argument(
        "--ga-seeds",
        type=parse_seed_list,
        default=parse_seed_list(DEFAULT_GA_SEEDS),
        help="Seed-matrix GA seeds. Must have same count as --scenario-seeds.",
    )
    parser.add_argument(
        "--scenario-seeds",
        type=parse_seed_list,
        default=parse_seed_list(DEFAULT_SCENARIO_SEEDS),
        help="Seed-matrix scenario-generation seeds. Must have same count as --ga-seeds.",
    )
    parser.add_argument(
        "--matrix-output-root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Seed-matrix parent directory for output packages.",
    )
    parser.add_argument(
        "--matrix-config-root",
        type=Path,
        default=DEFAULT_CONFIG_ROOT,
        help="Seed-matrix parent directory for generated seeded configs.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Write seeded configs and planned paths without launching runs.",
    )

    args = parser.parse_args()
    if args.scenario_config is None:
        args.scenario_config = [DEFAULT_SCENARIO_CONFIG]
    return args


def run_seed_matrix(args: argparse.Namespace) -> int:
    pairs = build_cycle_pairs(args.ga_seeds, args.scenario_seeds)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output_root = args.matrix_output_root.resolve() / stamp
    config_root = args.matrix_config_root.resolve() / stamp
    input_content_path = balance_package.resolve_package_content_path(
        args.input_package,
        runtime.DEFAULT_GA_CONTENT_DIR,
    )

    print(f"seed-pairs={len(pairs)}", flush=True)
    print(f"output-root={output_root}", flush=True)
    print(f"config-root={config_root}", flush=True)

    summary_rows: list[dict[str, Any]] = []
    for run_index, (ga_seed, scenario_seed) in enumerate(pairs, start=1):
        run_output_dir = make_unique_run_dir(output_root, run_index, ga_seed, scenario_seed)
        run_config_dir = config_root / run_output_dir.name
        seeded_ga_config = run_config_dir / "ga" / "full-genome.json"
        seeded_scenario_config = run_config_dir / "scenario" / "full-genome-scenario-eval.json"

        seed_ga_config(args.ga_config.resolve(), seeded_ga_config, ga_seed)
        if len(args.scenario_config) != 1:
            raise ValueError("Full-genome seed matrix currently expects exactly one scenario config.")
        seed_scenario_config(args.scenario_config[0].resolve(), seeded_scenario_config, scenario_seed)

        print(
            f"run={run_index}/{len(pairs)} ga_seed={ga_seed} "
            f"scenario_seed={scenario_seed} output={run_output_dir}",
            flush=True,
        )
        if args.dry_run:
            summary_rows.append(
                {
                    "run": run_index,
                    "gaSeed": ga_seed,
                    "scenarioSeed": scenario_seed,
                    "outputPackage": str(run_output_dir),
                    "gaConfig": str(seeded_ga_config),
                    "scenarioConfig": str(seeded_scenario_config),
                    "exitCode": 0,
                    "dryRun": True,
                }
            )
            continue

        run_args = SimpleNamespace(
            ga_config=seeded_ga_config,
            scenario_config=[seeded_scenario_config],
            balance_config=args.balance_config.resolve(),
        )
        config = load_config(run_args)
        exit_code = full_genome.run(
            config,
            source_content_path=input_content_path,
            output_package_path=run_output_dir,
            persist_results=False,
        )
        summary_rows.append(build_matrix_summary_row(run_index, ga_seed, scenario_seed, run_output_dir, exit_code))
        if exit_code != 0:
            write_matrix_summary(output_root, summary_rows)
            return exit_code

    write_matrix_summary(output_root, summary_rows)
    print("full-genome seed matrix complete", flush=True)
    return 0


def build_matrix_summary_row(
    run_index: int,
    ga_seed: int,
    scenario_seed: int,
    output_package: Path,
    exit_code: int,
) -> dict[str, Any]:
    row: dict[str, Any] = {
        "run": run_index,
        "gaSeed": ga_seed,
        "scenarioSeed": scenario_seed,
        "outputPackage": str(output_package),
        "exitCode": exit_code,
    }
    report_path = output_package / "report.json"
    if not report_path.is_file():
        return row

    report = load_json(report_path)
    evidence = report.get("evidence", {}) if isinstance(report, dict) else {}
    full_genome_evidence = evidence.get("full-genome", {}) if isinstance(evidence, dict) else {}
    metrics = full_genome_evidence.get("metrics", {}) if isinstance(full_genome_evidence, dict) else {}
    for metric_name, values in metrics.items():
        if not isinstance(values, dict):
            continue
        key = metric_name[0].lower() + metric_name[1:]
        row[f"{key}Before"] = values.get("before")
        row[f"{key}After"] = values.get("after")
        row[f"{key}Delta"] = values.get("delta")
    return row


def write_matrix_summary(output_root: Path, rows: list[dict[str, Any]]) -> None:
    write_json(output_root / "matrix-summary.json", {"runs": rows})
    lines = [
        "# Full-Genome Seed Matrix",
        "",
        "| Run | GA Seed | Scenario Seed | Fitness Delta | After Fitness | Win Rate | Turn Limit | Package |",
        "| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |",
    ]
    for row in rows:
        lines.append(
            "| "
            f"{row.get('run', '')} | "
            f"{row.get('gaSeed', '')} | "
            f"{row.get('scenarioSeed', '')} | "
            f"`{format_optional_float(row.get('fitnessDelta'))}` | "
            f"`{format_optional_float(row.get('fitnessAfter'))}` | "
            f"`{format_optional_percent(row.get('attackerWinRateAfter'))}` | "
            f"`{format_optional_percent(row.get('turnLimitRateAfter'))}` | "
            f"`{row.get('outputPackage', '')}` |"
        )
    (output_root / "matrix-summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def format_optional_float(value: Any) -> str:
    return "" if value is None else f"{float(value):.4f}"


def format_optional_percent(value: Any) -> str:
    return "" if value is None else f"{float(value):.2%}"


def main() -> int:
    try:
        runtime.disable_windows_quick_edit()
        return run_seed_matrix(parse_args())
    except KeyboardInterrupt:
        print("interrupted; stopping", flush=True)
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
