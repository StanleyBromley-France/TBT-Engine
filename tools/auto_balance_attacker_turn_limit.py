#!/usr/bin/env python3
"""Tune the attacker turn limit against generated eval scenarios."""
from __future__ import annotations

import argparse
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.cli import add_config_arguments, raise_direct_balancer_cli_error
from auto_balancer.config import load_balancer_config_from_args
from auto_balancer.measurement_models import AttackerTurnLimitMeasurement


class RepeatBatchEvaluator:
    def __init__(self, eval_config: eval_api.EvalCommandConfig, turn_budget: int):
        self._eval_config = eval_config
        self._turn_budget = turn_budget

    def evaluate_batch(self, repeat_count: int) -> tuple[int, int, int]:
        return eval_api.run_eval_with_repeat_count(
            self._eval_config,
            self._turn_budget,
            repeat_count,
        )


class AttackerTurnLimitCandidateEvaluator:
    def __init__(self, config: config_models.AttackerTurnLimitBalancerConfig, eval_config: eval_api.EvalCommandConfig):
        self._config = config
        self._eval_config = eval_config
        self._cache: dict[int, AttackerTurnLimitMeasurement] = {}

    def evaluate(self, turn_budget: int) -> AttackerTurnLimitMeasurement:
        normalized_turn_budget = ga.bounded_integer(
            turn_budget,
            self._config.balance.attacker_turn_limit_min,
            self._config.balance.attacker_turn_limit_max,
        )

        if normalized_turn_budget not in self._cache:
            self._cache[normalized_turn_budget] = measure_turn_budget_with_staged_repeats(
                normalized_turn_budget,
                self._config,
                self._config.ga.evaluation_repeat_stages,
                self._eval_config,
            )

        return self._cache[normalized_turn_budget]


def get_measurement_fitness(measurement: AttackerTurnLimitMeasurement) -> float:
    return measurement.fitness


def load_balancer_config(args: argparse.Namespace) -> config_models.AttackerTurnLimitBalancerConfig:
    return load_balancer_config_from_args(
        config_models.AttackerTurnLimitBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def validate_config(config: config_models.AttackerTurnLimitBalancerConfig) -> None:
    if config.balance.attacker_turn_limit_min <= 0:
        raise ValueError("Balancer config attacker_turn_limit_min must be positive.")
    if config.balance.attacker_turn_limit_max < config.balance.attacker_turn_limit_min:
        raise ValueError("Balancer config attacker_turn_limit_max must be greater than or equal to attacker_turn_limit_min.")
    if config.ga.candidate_population_size <= 0:
        raise ValueError("Balancer config candidate_population_size must be positive.")
    if config.ga.generation_count < 0:
        raise ValueError("Balancer config generation_count must be zero or greater.")
    if config.ga.evaluation_log_mode not in {"quiet", "normal", "verbose"}:
        raise ValueError("Balancer config evaluation_log_mode must be 'quiet', 'normal', or 'verbose'.")
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Balancer config generated_scenario_count must be positive.")
    if config.scenario.map_width_tiles <= 0 or config.scenario.map_height_tiles <= 0:
        raise ValueError("Balancer config map dimensions must be positive.")
    if config.balance.attacker_win_rate_target_min > config.balance.attacker_win_rate_target_max:
        raise ValueError("Balancer config attacker win-rate target bounds are invalid.")
    if config.balance.turn_limit_rate_target_min > config.balance.turn_limit_rate_target_max:
        raise ValueError("Balancer config turn-limit target bounds are invalid.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)


def build_eval_config(config: config_models.AttackerTurnLimitBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
    final_repeat_count = config.ga.evaluation_repeat_stages[-1].total_repeats
    return eval_api.create_eval_config(
        cli_path=None,
        content_path=content_path,
        game_state=config.scenario.game_state_id,
        validation=config.scenario.validation_mode,
        seed=config.scenario.evaluation_random_seed,
        repeat_count=final_repeat_count,
        timeout_seconds=config.ga.evaluation_timeout_seconds,
        log_mode=config.ga.evaluation_log_mode,
    )


def build_measurement(
    config: config_models.AttackerTurnLimitBalancerConfig,
    turn_budget: int,
    repeat_result: eval_api.RepeatScheduleResult,
) -> AttackerTurnLimitMeasurement:
    attacker_win_rate = repeat_result.win_rate
    turn_limit_rate = repeat_result.turn_limit_rate
    attacker_win_rate_confidence_min, attacker_win_rate_confidence_max = ga.compute_wilson_interval(repeat_result.wins, repeat_result.total_runs)
    win_rate_raw_fitness = ga.compute_target_band_fitness(
        attacker_win_rate,
        config.balance.attacker_win_rate_target_min,
        config.balance.attacker_win_rate_target_max,
    )
    turn_limit_raw_fitness = ga.compute_target_band_fitness(
        turn_limit_rate,
        config.balance.turn_limit_rate_target_min,
        config.balance.turn_limit_rate_target_max,
    )
    raw_fitness = (
        win_rate_raw_fitness * config.balance.attacker_win_rate_fitness_weight
        + turn_limit_raw_fitness * config.balance.turn_limit_rate_fitness_weight
    )
    win_rate_fitness = ga.compute_confidence_adjusted_target_band_fitness(
        attacker_win_rate,
        attacker_win_rate_confidence_min,
        attacker_win_rate_confidence_max,
        config.balance.attacker_win_rate_target_min,
        config.balance.attacker_win_rate_target_max,
    )
    turn_limit_fitness = ga.compute_target_band_fitness(
        turn_limit_rate,
        config.balance.turn_limit_rate_target_min,
        config.balance.turn_limit_rate_target_max,
    )
    fitness = (
        win_rate_fitness * config.balance.attacker_win_rate_fitness_weight
        + turn_limit_fitness * config.balance.turn_limit_rate_fitness_weight
    )
    return AttackerTurnLimitMeasurement(
        turn_budget=turn_budget,
        attacker_wins=repeat_result.wins,
        turn_limit_count=repeat_result.turn_limit_count,
        total_runs=repeat_result.total_runs,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        raw_fitness=raw_fitness,
        fitness=fitness,
        attacker_win_rate_confidence_min=attacker_win_rate_confidence_min,
        attacker_win_rate_confidence_max=attacker_win_rate_confidence_max,
    )


def prepare_eval_content(config: config_models.AttackerTurnLimitBalancerConfig) -> Path:
    scenario_config = scenarios.ScenarioGenerationConfig(
        seed=config.scenario.scenario_generation_random_seed,
        generated_scenarios_per_run=config.scenario.generated_scenario_count,
        map_width=config.scenario.map_width_tiles,
        map_height=config.scenario.map_height_tiles,
    )
    generated_content_path = build_generated_content_path(config)
    return scenarios.prepare_generated_content(
        source_content_path=runtime.DEFAULT_GA_CONTENT_DIR,
        generated_content_path=generated_content_path,
        config=scenario_config,
    )


def build_generated_content_path(config: config_models.AttackerTurnLimitBalancerConfig) -> Path:
    return scenarios.build_generated_content_path(
        runtime.DEFAULT_GA_CONTENT_DIR,
        config.scenario.scenario_generation_random_seed,
        config.scenario.generated_scenario_count,
    )


def measure_turn_budget_with_staged_repeats(
    turn_budget: int,
    config: config_models.AttackerTurnLimitBalancerConfig,
    repeat_stages: tuple[eval_api.RepeatStage, ...],
    eval_config: eval_api.EvalCommandConfig,
) -> AttackerTurnLimitMeasurement:
    batch_evaluator = RepeatBatchEvaluator(eval_config, turn_budget)
    repeat_result = eval_api.run_staged_repeat_schedule(
        repeat_stages,
        batch_evaluator.evaluate_batch,
        should_promote=lambda repeat_result: should_promote_repeat_result(config, repeat_result),
    )
    return build_measurement(config, turn_budget, repeat_result)


def print_candidate_measurement(measurement: AttackerTurnLimitMeasurement) -> None:
    print(
        "candidate "
        f"max-turns={measurement.turn_budget} "
        f"attacker-win-rate={measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"wins={measurement.attacker_wins}/{measurement.total_runs} "
        f"fitness={measurement.fitness:.4f}",
        flush=True,
    )


def print_generation_best(generation: int, measurement: AttackerTurnLimitMeasurement) -> None:
    print(
        "generation "
        f"{generation} best "
        f"max-turns={measurement.turn_budget} "
        f"attacker-win-rate={measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"fitness={measurement.fitness:.4f}",
        flush=True,
    )


def should_promote_repeat_result(
    config: config_models.AttackerTurnLimitBalancerConfig,
    repeat_result: eval_api.RepeatScheduleResult,
) -> bool:
    attacker_win_rate_confidence_min, attacker_win_rate_confidence_max = ga.compute_wilson_interval(repeat_result.wins, repeat_result.total_runs)
    return ga.interval_overlaps_target_band(
        attacker_win_rate_confidence_min,
        attacker_win_rate_confidence_max,
        config.balance.attacker_win_rate_target_min,
        config.balance.attacker_win_rate_target_max,
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Tune the attacker turn limit.")
    add_config_arguments(parser)
    return parser.parse_args()


def run(config: config_models.AttackerTurnLimitBalancerConfig) -> int:
    runtime.ensure_deap_available()

    # Build the generated content once, then evaluate each GA candidate against that same scenario set.
    validate_config(config)
    content_path = prepare_eval_content(config)
    eval_config = build_eval_config(config, content_path)
    candidate_evaluator = AttackerTurnLimitCandidateEvaluator(config, eval_config)
    ga_config = ga.IntegerGaConfig(
        seed=config.ga.ga_random_seed,
        minimum=config.balance.attacker_turn_limit_min,
        maximum=config.balance.attacker_turn_limit_max,
        initial_value=config.balance.initial_attacker_turn_limit,
        population_size=config.ga.candidate_population_size,
        generations=config.ga.generation_count,
        mutation_probability=config.ga.mutation_probability,
    )

    best_measurement = ga.run_integer_ga(
        config=ga_config,
        mutate_gene=ga.mutate_integer_gene,
        evaluate_candidate=candidate_evaluator.evaluate,
        get_fitness=get_measurement_fitness,
        on_candidate=print_candidate_measurement,
        on_generation_best=print_generation_best,
    )

    print(
        "best "
        f"max-turns={best_measurement.turn_budget} "
        f"attacker-win-rate={best_measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={best_measurement.turn_limit_rate:.2%} "
        f"wins={best_measurement.attacker_wins}/{best_measurement.total_runs} "
        f"fitness={best_measurement.fitness:.4f}",
        flush=True,
    )
    print(f"cli={eval_config.cli_path}", flush=True)
    print(f"content={eval_config.content_path}", flush=True)
    return 0


def main() -> int:
    return raise_direct_balancer_cli_error()


if __name__ == "__main__":
    raise SystemExit(main())
