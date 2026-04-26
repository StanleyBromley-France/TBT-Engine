#!/usr/bin/env python3
"""Tune generated map terrain distribution using eval telemetry."""
from __future__ import annotations

import argparse
import random
from pathlib import Path

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.measurement_models as measurements
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.cli import add_config_arguments
from auto_balancer.config import load_balancer_config_from_args


class TerrainDistributionCandidateEvaluator:
    def __init__(
        self,
        config: config_models.TerrainDistributionBalancerConfig,
        eval_config: eval_api.EvalCommandConfig,
        content_path: Path,
        offensive_ability_ids: set[str],
    ):
        self._config = config
        self._eval_config = eval_config
        self._content_path = content_path
        self._offensive_ability_ids = offensive_ability_ids
        self._cache: dict[tuple[int, int], measurements.TerrainDistributionMeasurement] = {}

    def evaluate(self, mountain_tile_percent: int, water_tile_percent: int) -> measurements.TerrainDistributionMeasurement:
        normalized_mountain, normalized_water, normalized_plain = normalize_distribution_candidate(
            mountain_tile_percent,
            water_tile_percent,
            self._config,
        )
        cache_key = (normalized_mountain, normalized_water)
        if cache_key not in self._cache:
            self._cache[cache_key] = evaluate_terrain_distribution_candidate(
                normalized_mountain,
                normalized_water,
                normalized_plain,
                self._config,
                self._eval_config,
                self._content_path,
                self._offensive_ability_ids,
        )
        return self._cache[cache_key]


def load_balancer_config(args: argparse.Namespace) -> config_models.TerrainDistributionBalancerConfig:
    return load_balancer_config_from_args(
        config_models.TerrainDistributionBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def validate_config(config: config_models.TerrainDistributionBalancerConfig) -> None:
    if config.scenario.map_width_tiles <= 0 or config.scenario.map_height_tiles <= 0:
        raise ValueError("Terrain distribution balancer map dimensions must be positive.")
    if config.ga.evaluation_turn_budget <= 0:
        raise ValueError("Terrain distribution balancer evaluation_turn_budget must be positive.")
    if config.ga.candidate_population_size <= 0:
        raise ValueError("Terrain distribution balancer candidate_population_size must be positive.")
    if config.ga.generation_count < 0:
        raise ValueError("Terrain distribution balancer generation_count must be zero or greater.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    if config.ga.evaluation_log_mode not in {"quiet", "normal", "verbose"}:
        raise ValueError("Terrain distribution balancer evaluation_log_mode must be 'quiet', 'normal', or 'verbose'.")
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Terrain distribution balancer generated_scenario_count must be positive.")
    if not (0 <= config.balance.mountain_tile_percent_min <= config.balance.mountain_tile_percent_max):
        raise ValueError("Terrain distribution balancer mountain bounds are invalid.")
    if not (0 <= config.balance.water_tile_percent_min <= config.balance.water_tile_percent_max):
        raise ValueError("Terrain distribution balancer water bounds are invalid.")
    if config.balance.plain_tile_percent_min <= 0 or config.balance.plain_tile_percent_min >= 100:
        raise ValueError("Terrain distribution balancer plain_tile_percent_min must be between 1 and 99.")
    validate_target_bounds(config)


def validate_target_bounds(config: config_models.TerrainDistributionBalancerConfig) -> None:
    target_bounds = [
        (config.balance.turn_limit_rate_target_min, config.balance.turn_limit_rate_target_max, "turn-limit"),
        (config.balance.average_attacker_turn_count_target_min, config.balance.average_attacker_turn_count_target_max, "average-attacker-turn-count"),
        (config.balance.average_action_count_target_min, config.balance.average_action_count_target_max, "action-count"),
        (config.balance.move_action_rate_target_min, config.balance.move_action_rate_target_max, "move-rate"),
        (config.balance.skip_action_rate_target_min, config.balance.skip_action_rate_target_max, "skip-rate"),
        (config.balance.offensive_ability_use_rate_target_min, config.balance.offensive_ability_use_rate_target_max, "offensive-rate"),
        (config.balance.attacker_win_rate_target_min, config.balance.attacker_win_rate_target_max, "win-rate"),
    ]
    for target_low, target_high, target_name in target_bounds:
        if target_low > target_high:
            raise ValueError(f"Terrain distribution balancer {target_name} target bounds are invalid.")


def build_eval_config(config: config_models.TerrainDistributionBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
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


def prepare_eval_content(config: config_models.TerrainDistributionBalancerConfig) -> Path:
    scenario_config = scenarios.ScenarioGenerationConfig(
        seed=config.scenario.scenario_generation_random_seed,
        generated_scenarios_per_run=config.scenario.generated_scenario_count,
        map_width=config.scenario.map_width_tiles,
        map_height=config.scenario.map_height_tiles,
    )
    generated_content_path = scenarios.build_generated_content_path(
        runtime.DEFAULT_GA_CONTENT_DIR,
        config.scenario.scenario_generation_random_seed,
        config.scenario.generated_scenario_count,
    )
    return scenarios.prepare_generated_content(
        source_content_path=runtime.DEFAULT_GA_CONTENT_DIR,
        generated_content_path=generated_content_path,
        config=scenario_config,
    )


def normalize_distribution_candidate(
    mountain_tile_percent: int,
    water_tile_percent: int,
    config: config_models.TerrainDistributionBalancerConfig,
) -> tuple[int, int, int]:
    mountain = max(config.balance.mountain_tile_percent_min, min(config.balance.mountain_tile_percent_max, int(mountain_tile_percent)))
    water = max(config.balance.water_tile_percent_min, min(config.balance.water_tile_percent_max, int(water_tile_percent)))

    max_combined_obstacles = 100 - config.balance.plain_tile_percent_min
    if mountain + water > max_combined_obstacles:
        total_obstacles = mountain + water
        if total_obstacles > 0:
            scale = max_combined_obstacles / total_obstacles
            mountain = int(round(mountain * scale))
            water = int(round(water * scale))

        while mountain + water > max_combined_obstacles:
            if mountain >= water and mountain > config.balance.mountain_tile_percent_min:
                mountain -= 1
                continue
            if water > config.balance.water_tile_percent_min:
                water -= 1
                continue
            break

    plain = 100 - mountain - water
    if plain < config.balance.plain_tile_percent_min:
        deficit = config.balance.plain_tile_percent_min - plain
        while deficit > 0 and (mountain > config.balance.mountain_tile_percent_min or water > config.balance.water_tile_percent_min):
            if mountain >= water and mountain > config.balance.mountain_tile_percent_min:
                mountain -= 1
            elif water > config.balance.water_tile_percent_min:
                water -= 1
            deficit -= 1
        plain = 100 - mountain - water

    return mountain, water, plain


def build_terrain_distribution_spec(mountain_tile_percent: int, water_tile_percent: int, plain_tile_percent: int) -> dict[str, float]:
    total_percent = mountain_tile_percent + water_tile_percent + plain_tile_percent
    if total_percent <= 0:
        raise ValueError("Tile distribution total percent must be positive.")

    distribution: dict[str, float] = {}
    if plain_tile_percent > 0:
        distribution["Plain"] = plain_tile_percent / total_percent
    if mountain_tile_percent > 0:
        distribution["Mountain"] = mountain_tile_percent / total_percent
    if water_tile_percent > 0:
        distribution["Water"] = water_tile_percent / total_percent
    return distribution


def evaluate_terrain_distribution_candidate(
    mountain_tile_percent: int,
    water_tile_percent: int,
    plain_tile_percent: int,
    config: config_models.TerrainDistributionBalancerConfig,
    eval_config: eval_api.EvalCommandConfig,
    content_path: Path,
    offensive_ability_ids: set[str],
    repeat_stages: tuple[eval_api.RepeatStage, ...] | None = None,
) -> measurements.TerrainDistributionMeasurement:
    apply_distribution_to_content(content_path, mountain_tile_percent, water_tile_percent, plain_tile_percent)
    staged_repeats = config.ga.evaluation_repeat_stages if repeat_stages is None else repeat_stages

    try:
        summary = run_eval_detailed_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            staged_repeats,
            offensive_ability_ids,
        )
    except Exception as exc:  # pragma: no cover - defensive fallback for eval/runtime failures
        total_runs = staged_repeats[-1].total_repeats * config.scenario.generated_scenario_count
        return measurements.TerrainDistributionMeasurement(
            mountain_tile_percent=mountain_tile_percent,
            water_tile_percent=water_tile_percent,
            plain_tile_percent=plain_tile_percent,
            attacker_wins=0,
            turn_limit_count=total_runs,
            total_runs=total_runs,
            attacker_win_rate=0.0,
            turn_limit_rate=1.0,
            average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
            average_action_count=float(config.ga.evaluation_turn_budget * 6),
            move_action_rate=1.0,
            skip_action_rate=1.0,
            ability_use_rate=0.0,
            offensive_ability_use_rate=0.0,
            support_ability_use_rate=0.0,
            raw_fitness=-10.0,
            fitness=-10.0,
            attacker_win_rate_confidence_min=0.0,
            attacker_win_rate_confidence_max=1.0,
            error_message=str(exc),
        )

    return build_measurement(config, mountain_tile_percent, water_tile_percent, plain_tile_percent, summary)


def run_eval_detailed_with_stages(
    eval_config: eval_api.EvalCommandConfig,
    turn_budget: int,
    repeat_stages: tuple[eval_api.RepeatStage, ...],
    offensive_ability_ids: set[str],
) -> eval_api.EvalDetailedSummary:
    return eval_api.run_staged_total_repeat_schedule(
        repeat_stages,
        lambda repeat_count: eval_api.run_eval_detailed(
            eval_config.with_repeat_count(repeat_count),
            turn_budget,
            offensive_ability_ids,
        ),
    )


def apply_distribution_to_content(
    content_path: Path,
    mountain_tile_percent: int,
    water_tile_percent: int,
    plain_tile_percent: int,
) -> None:
    terrain_distribution = build_terrain_distribution_spec(mountain_tile_percent, water_tile_percent, plain_tile_percent)
    scenarios.update_tile_distribution_for_game_states(content_path, terrain_distribution)


def build_measurement(
    config: config_models.TerrainDistributionBalancerConfig,
    mountain_tile_percent: int,
    water_tile_percent: int,
    plain_tile_percent: int,
    summary,
) -> measurements.TerrainDistributionMeasurement:
    attacker_win_rate = summary.attacker_wins / summary.total_runs
    turn_limit_rate = summary.turn_limit_count / summary.total_runs
    attacker_win_rate_confidence_min, attacker_win_rate_confidence_max = ga.compute_wilson_interval(summary.attacker_wins, summary.total_runs)

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
    attacker_turns_raw_fitness = ga.compute_target_band_fitness(
        summary.average_attacker_turn_count,
        config.balance.average_attacker_turn_count_target_min,
        config.balance.average_attacker_turn_count_target_max,
    )
    action_count_raw_fitness = ga.compute_target_band_fitness(
        summary.average_action_count,
        config.balance.average_action_count_target_min,
        config.balance.average_action_count_target_max,
    )
    move_rate_raw_fitness = ga.compute_target_band_fitness(
        summary.move_action_rate,
        config.balance.move_action_rate_target_min,
        config.balance.move_action_rate_target_max,
    )
    skip_rate_raw_fitness = ga.compute_target_band_fitness(
        summary.skip_action_rate,
        config.balance.skip_action_rate_target_min,
        config.balance.skip_action_rate_target_max,
    )
    offensive_ability_raw_fitness = ga.compute_target_band_fitness(
        summary.offensive_ability_use_rate,
        config.balance.offensive_ability_use_rate_target_min,
        config.balance.offensive_ability_use_rate_target_max,
    )

    raw_fitness = (
        turn_limit_raw_fitness * config.balance.turn_limit_rate_fitness_weight
        + attacker_turns_raw_fitness * config.balance.average_attacker_turn_count_fitness_weight
        + action_count_raw_fitness * config.balance.average_action_count_fitness_weight
        + move_rate_raw_fitness * config.balance.move_action_rate_fitness_weight
        + skip_rate_raw_fitness * config.balance.skip_action_rate_fitness_weight
        + offensive_ability_raw_fitness * config.balance.offensive_ability_use_rate_fitness_weight
        + win_rate_raw_fitness * config.balance.attacker_win_rate_fitness_weight
    )

    win_rate_fitness = ga.compute_confidence_adjusted_target_band_fitness(
        attacker_win_rate,
        attacker_win_rate_confidence_min,
        attacker_win_rate_confidence_max,
        config.balance.attacker_win_rate_target_min,
        config.balance.attacker_win_rate_target_max,
    )
    fitness = (
        turn_limit_raw_fitness * config.balance.turn_limit_rate_fitness_weight
        + attacker_turns_raw_fitness * config.balance.average_attacker_turn_count_fitness_weight
        + action_count_raw_fitness * config.balance.average_action_count_fitness_weight
        + move_rate_raw_fitness * config.balance.move_action_rate_fitness_weight
        + skip_rate_raw_fitness * config.balance.skip_action_rate_fitness_weight
        + offensive_ability_raw_fitness * config.balance.offensive_ability_use_rate_fitness_weight
        + win_rate_fitness * config.balance.attacker_win_rate_fitness_weight
    )

    return measurements.TerrainDistributionMeasurement(
        mountain_tile_percent=mountain_tile_percent,
        water_tile_percent=water_tile_percent,
        plain_tile_percent=plain_tile_percent,
        attacker_wins=summary.attacker_wins,
        turn_limit_count=summary.turn_limit_count,
        total_runs=summary.total_runs,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=summary.average_attacker_turn_count,
        average_action_count=summary.average_action_count,
        move_action_rate=summary.move_action_rate,
        skip_action_rate=summary.skip_action_rate,
        ability_use_rate=summary.ability_use_rate,
        offensive_ability_use_rate=summary.offensive_ability_use_rate,
        support_ability_use_rate=summary.support_ability_use_rate,
        raw_fitness=raw_fitness,
        fitness=fitness,
        attacker_win_rate_confidence_min=attacker_win_rate_confidence_min,
        attacker_win_rate_confidence_max=attacker_win_rate_confidence_max,
        error_message=None,
    )


def build_initial_population(config: config_models.TerrainDistributionBalancerConfig, rng: random.Random, individual_type: type) -> list:
    max_combined_obstacles = 100 - config.balance.plain_tile_percent_min
    seed_candidates = [
        (0, 0),
        (config.balance.initial_mountain_tile_percent, config.balance.initial_water_tile_percent),
        (config.balance.mountain_tile_percent_max, 0),
        (0, config.balance.water_tile_percent_max),
        (max_combined_obstacles // 2, max_combined_obstacles // 3),
    ]

    population: list = []
    seen: set[tuple[int, int]] = set()
    for mountain_tile_percent, water_tile_percent in seed_candidates:
        candidate = normalize_distribution_candidate(mountain_tile_percent, water_tile_percent, config)[:2]
        if candidate in seen:
            continue
        seen.add(candidate)
        population.append(individual_type(list(candidate)))

    while len(population) < config.ga.candidate_population_size:
        candidate = normalize_distribution_candidate(
            rng.randint(config.balance.mountain_tile_percent_min, config.balance.mountain_tile_percent_max),
            rng.randint(config.balance.water_tile_percent_min, config.balance.water_tile_percent_max),
            config,
        )[:2]
        population.append(individual_type(list(candidate)))

    return population[: config.ga.candidate_population_size]


def mutate_distribution_candidate(individual: list[int], config: config_models.TerrainDistributionBalancerConfig, rng: random.Random):
    mountain_tile_percent, water_tile_percent = individual[0], individual[1]
    if rng.random() < 0.5:
        mountain_tile_percent += rng.randint(-8, 8)
    else:
        mountain_tile_percent = rng.randint(config.balance.mountain_tile_percent_min, config.balance.mountain_tile_percent_max)

    if rng.random() < 0.5:
        water_tile_percent += rng.randint(-6, 6)
    else:
        water_tile_percent = rng.randint(config.balance.water_tile_percent_min, config.balance.water_tile_percent_max)

    normalized_mountain, normalized_water, _ = normalize_distribution_candidate(
        mountain_tile_percent,
        water_tile_percent,
        config,
    )
    individual[0] = normalized_mountain
    individual[1] = normalized_water
    return (individual,)


def run_terrain_distribution_ga(
    config: config_models.TerrainDistributionBalancerConfig,
    candidate_evaluator: TerrainDistributionCandidateEvaluator,
) -> measurements.TerrainDistributionMeasurement:
    from deap import base, creator, tools

    if not hasattr(creator, "TerrainDistributionFitnessMax"):
        creator.create("TerrainDistributionFitnessMax", base.Fitness, weights=(1.0,))
    if not hasattr(creator, "TerrainDistributionIndividual"):
        creator.create("TerrainDistributionIndividual", list, fitness=creator.TerrainDistributionFitnessMax)

    rng = random.Random(config.ga.ga_random_seed)
    toolbox = base.Toolbox()
    toolbox.register("select", tools.selTournament, tournsize=3)
    toolbox.register("mutate", mutate_distribution_candidate, config=config, rng=rng)

    measurement_cache: dict[tuple[int, int], measurements.TerrainDistributionMeasurement] = {}
    hall_of_fame = tools.HallOfFame(1)

    def evaluate_individual(individual: list[int]) -> tuple[float]:
        normalized_mountain, normalized_water, _ = normalize_distribution_candidate(
            individual[0],
            individual[1],
            config,
        )
        individual[0] = normalized_mountain
        individual[1] = normalized_water
        cache_key = (normalized_mountain, normalized_water)

        if cache_key not in measurement_cache:
            measurement_cache[cache_key] = candidate_evaluator.evaluate(normalized_mountain, normalized_water)

        measurement = measurement_cache[cache_key]
        print_candidate_measurement(measurement)
        return (measurement.fitness,)

    toolbox.register("evaluate", evaluate_individual)

    population = build_initial_population(config, rng, creator.TerrainDistributionIndividual)
    ga.evaluate_invalid_individuals(population, toolbox.evaluate)
    hall_of_fame.update(population)

    for generation in range(1, config.ga.generation_count + 1):
        offspring = list(map(toolbox.clone, toolbox.select(population, len(population))))

        for individual in offspring:
            if rng.random() <= config.ga.mutation_probability:
                toolbox.mutate(individual)
                del individual.fitness.values

        ga.evaluate_invalid_individuals(offspring, toolbox.evaluate)
        population[:] = offspring
        hall_of_fame.update(population)

        generation_best = measurement_cache[(hall_of_fame[0][0], hall_of_fame[0][1])]
        print_generation_best(generation, generation_best)

    best_mountain, best_water = hall_of_fame[0][0], hall_of_fame[0][1]
    return measurement_cache[(best_mountain, best_water)]


def print_candidate_measurement(measurement: measurements.TerrainDistributionMeasurement) -> None:
    message = (
        "candidate "
        f"terrain={format_distribution(measurement)} "
        f"win-rate={measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={measurement.average_attacker_turn_count:.2f} "
        f"avg-actions={measurement.average_action_count:.2f} "
        f"move-rate={measurement.move_action_rate:.2%} "
        f"skip-rate={measurement.skip_action_rate:.2%} "
        f"offensive-rate={measurement.offensive_ability_use_rate:.2%} "
        f"fitness={measurement.fitness:.4f}"
    )
    if measurement.error_message:
        message += f" error={measurement.error_message}"
    print(message, flush=True)


def print_generation_best(generation: int, measurement: measurements.TerrainDistributionMeasurement) -> None:
    print(
        "generation "
        f"{generation} best "
        f"terrain={format_distribution(measurement)} "
        f"win-rate={measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={measurement.average_attacker_turn_count:.2f} "
        f"move-rate={measurement.move_action_rate:.2%} "
        f"skip-rate={measurement.skip_action_rate:.2%} "
        f"fitness={measurement.fitness:.4f}",
        flush=True,
    )


def format_distribution(measurement: measurements.TerrainDistributionMeasurement) -> str:
    return (
        f"plain={measurement.plain_tile_percent}%/"
        f"mountain={measurement.mountain_tile_percent}%/"
        f"water={measurement.water_tile_percent}%"
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Tune generated map terrain distribution.")
    add_config_arguments(parser)
    return parser.parse_args()


def run(config: config_models.TerrainDistributionBalancerConfig) -> int:
    runtime.ensure_local_deap()

    # Each candidate edits the generated content pack, runs eval, then the best confirmed candidate is written back.
    validate_config(config)
    content_path = prepare_eval_content(config)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    candidate_evaluator = TerrainDistributionCandidateEvaluator(config, eval_config, content_path, offensive_ability_ids)

    best_measurement = run_terrain_distribution_ga(config, candidate_evaluator)
    # Candidate evaluation edits the working content pack each time, so leave it on the selected result.
    apply_distribution_to_content(
        content_path,
        best_measurement.mountain_tile_percent,
        best_measurement.water_tile_percent,
        best_measurement.plain_tile_percent,
    )
    print(
        "best "
        f"terrain={format_distribution(best_measurement)} "
        f"win-rate={best_measurement.attacker_win_rate:.2%} "
        f"turn-limit-rate={best_measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={best_measurement.average_attacker_turn_count:.2f} "
        f"avg-actions={best_measurement.average_action_count:.2f} "
        f"move-rate={best_measurement.move_action_rate:.2%} "
        f"skip-rate={best_measurement.skip_action_rate:.2%} "
        f"offensive-rate={best_measurement.offensive_ability_use_rate:.2%} "
        f"fitness={best_measurement.fitness:.4f}",
        flush=True,
    )
    print(f"turn-count={config.ga.evaluation_turn_budget}", flush=True)
    print(f"cli={eval_config.cli_path}", flush=True)
    print(f"content={eval_config.content_path}", flush=True)
    return 0


def main() -> int:
    return run(load_balancer_config(parse_args()))


if __name__ == "__main__":
    raise SystemExit(main())
