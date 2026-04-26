#!/usr/bin/env python3
"""Tune unit template stats for one primary role using eval telemetry."""
from __future__ import annotations

import argparse
import random
from pathlib import Path
from typing import Callable

import auto_balancer.config_models as config_models
import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.measurement_models as measurements
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.cli import add_config_arguments
from auto_balancer.config import load_balancer_config_from_args
from balancing_scripts.primary_roles.common import mean
from balancing_scripts.primary_roles.scoring import compute_primary_role_score


class PrimaryRoleCandidateEvaluator:
    def __init__(
        self,
        config: config_models.PrimaryRoleBalancerConfig,
        content_path: Path,
        eval_config: eval_api.EvalCommandConfig,
        offensive_ability_ids: set[str],
    ):
        self._config = config
        self._content_path = content_path
        self._eval_config = eval_config
        self._offensive_ability_ids = offensive_ability_ids
        self._cache: dict[tuple[int, int, int, int, int], measurements.PrimaryRoleMeasurement] = {}

    def evaluate(self, candidate: tuple[int, int, int, int, int]) -> measurements.PrimaryRoleMeasurement:
        normalized_candidate = normalize_candidate(self._config, candidate)
        if normalized_candidate not in self._cache:
            self._cache[normalized_candidate] = evaluate_candidate(
                self._config,
                self._content_path,
                self._eval_config,
                self._offensive_ability_ids,
                normalized_candidate,
        )
        return self._cache[normalized_candidate]


def load_balancer_config(args: argparse.Namespace) -> config_models.PrimaryRoleBalancerConfig:
    return load_balancer_config_from_args(
        config_models.PrimaryRoleBalancerConfig,
        args,
        repeat_stage_fields=("evaluation_repeat_stages",),
    )


def validate_config(config: config_models.PrimaryRoleBalancerConfig) -> None:
    if not config.balance.target_primary_role:
        raise ValueError("Primary role balancer target_primary_role must be set.")
    if config.scenario.map_width_tiles <= 0 or config.scenario.map_height_tiles <= 0:
        raise ValueError("Primary role balancer map dimensions must be positive.")
    if config.ga.evaluation_turn_budget <= 0:
        raise ValueError("Primary role balancer evaluation_turn_budget must be positive.")
    if config.ga.candidate_population_size <= 0 or config.ga.generation_count < 0:
        raise ValueError("Primary role balancer population config is invalid.")
    eval_api.validate_repeat_stages(config.ga.evaluation_repeat_stages)
    if config.scenario.generated_scenario_count <= 0:
        raise ValueError("Primary role balancer generated_scenario_count must be positive.")
    if config.ga.evaluation_log_mode not in {"quiet", "normal", "verbose"}:
        raise ValueError("Primary role balancer evaluation_log_mode must be 'quiet', 'normal', or 'verbose'.")
    validate_target_bounds(config)


def validate_target_bounds(config: config_models.PrimaryRoleBalancerConfig) -> None:
    target_bounds = [
        (config.balance.turn_limit_rate_target_min, config.balance.turn_limit_rate_target_max, "turn-limit"),
        (config.balance.average_attacker_turn_count_target_min, config.balance.average_attacker_turn_count_target_max, "average-attacker-turn-count"),
        (config.balance.average_action_count_target_min, config.balance.average_action_count_target_max, "action-count"),
    ]
    for target_low, target_high, target_name in target_bounds:
        if target_low > target_high:
            raise ValueError(f"Primary role balancer {target_name} target bounds are invalid.")


def prepare_eval_content(config: config_models.PrimaryRoleBalancerConfig) -> Path:
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


def build_eval_config(config: config_models.PrimaryRoleBalancerConfig, content_path: Path) -> eval_api.EvalCommandConfig:
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


def load_initial_candidate(config: config_models.PrimaryRoleBalancerConfig, content_path: Path) -> tuple[int, int, int, int, int]:
    unit_templates = scenarios.load_unit_templates(content_path)
    matched_units = [
        unit_template
        for unit_template in unit_templates
        if unit_template.get("primaryRole") == config.balance.target_primary_role
    ]
    if not matched_units:
        raise ValueError(f"No units matched primaryRole={config.balance.target_primary_role!r}.")

    return (
        round(mean(int(unit["maxHP"]) for unit in matched_units)),
        round(mean(int(unit["maxManaPoints"]) for unit in matched_units)),
        round(mean(int(unit["movePoints"]) for unit in matched_units)),
        round(mean(int(unit["physicalDamageReceived"]) for unit in matched_units)),
        round(mean(int(unit["magicDamageReceived"]) for unit in matched_units)),
    )


def normalize_candidate(
    config: config_models.PrimaryRoleBalancerConfig,
    candidate: tuple[int, int, int, int, int],
) -> tuple[int, int, int, int, int]:
    unit_max_hp, unit_max_mana_points, unit_move_points, physical_damage_received_percent, magic_damage_received_percent = candidate
    return (
        ga.bounded_integer(unit_max_hp, config.balance.unit_max_hp_min, config.balance.unit_max_hp_max),
        ga.bounded_integer(unit_max_mana_points, config.balance.unit_max_mana_points_min, config.balance.unit_max_mana_points_max),
        ga.bounded_integer(unit_move_points, config.balance.unit_move_points_min, config.balance.unit_move_points_max),
        ga.bounded_integer(
            physical_damage_received_percent,
            config.balance.physical_damage_received_percent_min,
            config.balance.physical_damage_received_percent_max,
        ),
        ga.bounded_integer(
            magic_damage_received_percent,
            config.balance.magic_damage_received_percent_min,
            config.balance.magic_damage_received_percent_max,
        ),
    )


def build_field_values(candidate: tuple[int, int, int, int, int]) -> dict[str, int]:
    unit_max_hp, unit_max_mana_points, unit_move_points, physical_damage_received_percent, magic_damage_received_percent = candidate
    return {
        "maxHP": unit_max_hp,
        "maxManaPoints": unit_max_mana_points,
        "movePoints": unit_move_points,
        "physicalDamageReceived": physical_damage_received_percent,
        "magicDamageReceived": magic_damage_received_percent,
    }


def evaluate_candidate(
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
    candidate: tuple[int, int, int, int, int],
    repeat_stages: tuple[eval_api.RepeatStage, ...] | None = None,
) -> measurements.PrimaryRoleMeasurement:
    normalized_candidate = normalize_candidate(config, candidate)
    apply_candidate_to_content(config, content_path, normalized_candidate)
    staged_repeats = config.ga.evaluation_repeat_stages if repeat_stages is None else repeat_stages

    try:
        summary = run_eval_role_alignment_with_stages(
            eval_config,
            config.ga.evaluation_turn_budget,
            staged_repeats,
            offensive_ability_ids,
        )
    except Exception as exc:  # pragma: no cover
        return measurements.PrimaryRoleMeasurement(
            *normalized_candidate,
            attacker_win_rate=0.0,
            turn_limit_rate=1.0,
            average_attacker_turn_count=float(config.ga.evaluation_turn_budget),
            average_action_count=float(config.ga.evaluation_turn_budget * 6),
            primary_role_alignment_score=-10.0,
            raw_fitness=-10.0,
            fitness=-10.0,
            error_message=str(exc),
        )

    return build_measurement(config, summary, normalized_candidate)


def run_eval_role_alignment_with_stages(
    eval_config: eval_api.EvalCommandConfig,
    turn_budget: int,
    repeat_stages: tuple[eval_api.RepeatStage, ...],
    offensive_ability_ids: set[str],
) -> eval_api.EvalRoleAlignmentSummary:
    return eval_api.run_staged_total_repeat_schedule(
        repeat_stages,
        lambda repeat_count: eval_api.run_eval_role_alignment(
            eval_config.with_repeat_count(repeat_count),
            turn_budget,
            offensive_ability_ids,
        ),
    )


def apply_candidate_to_content(
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    candidate: tuple[int, int, int, int, int],
) -> None:
    scenarios.update_unit_templates_for_role(
        content_path,
        config.balance.target_primary_role,
        None,
        build_field_values(normalize_candidate(config, candidate)),
    )


def build_measurement(
    config: config_models.PrimaryRoleBalancerConfig,
    summary: eval_api.EvalRoleAlignmentSummary,
    candidate: tuple[int, int, int, int, int],
) -> measurements.PrimaryRoleMeasurement:
    detailed = summary.detailed
    attacker_win_rate = detailed.attacker_wins / detailed.total_runs
    turn_limit_rate = detailed.turn_limit_count / detailed.total_runs
    primary_role_alignment_score = compute_primary_role_score(config.balance.target_primary_role, summary)

    turn_limit_fitness = ga.compute_target_band_fitness(
        turn_limit_rate,
        config.balance.turn_limit_rate_target_min,
        config.balance.turn_limit_rate_target_max,
    )
    attacker_turns_fitness = ga.compute_target_band_fitness(
        detailed.average_attacker_turn_count,
        config.balance.average_attacker_turn_count_target_min,
        config.balance.average_attacker_turn_count_target_max,
    )
    action_count_fitness = ga.compute_target_band_fitness(
        detailed.average_action_count,
        config.balance.average_action_count_target_min,
        config.balance.average_action_count_target_max,
    )

    raw_fitness = (
        turn_limit_fitness * config.balance.turn_limit_rate_fitness_weight
        + attacker_turns_fitness * config.balance.average_attacker_turn_count_fitness_weight
        + action_count_fitness * config.balance.average_action_count_fitness_weight
        + primary_role_alignment_score * config.balance.primary_role_alignment_fitness_weight
    )

    return measurements.PrimaryRoleMeasurement(
        *candidate,
        attacker_win_rate=attacker_win_rate,
        turn_limit_rate=turn_limit_rate,
        average_attacker_turn_count=detailed.average_attacker_turn_count,
        average_action_count=detailed.average_action_count,
        primary_role_alignment_score=primary_role_alignment_score,
        raw_fitness=raw_fitness,
        fitness=raw_fitness,
        error_message=None,
    )

def build_initial_population(
    config: config_models.PrimaryRoleBalancerConfig,
    initial_candidate: tuple[int, int, int, int, int],
    rng: random.Random,
    individual_type: type,
) -> list:
    seed_candidates = [
        initial_candidate,
        (config.balance.unit_max_hp_max, initial_candidate[1], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
        (config.balance.unit_max_hp_min, initial_candidate[1], initial_candidate[2], initial_candidate[3], initial_candidate[4]),
    ]
    population: list = []
    seen: set[tuple[int, int, int, int, int]] = set()
    for candidate in seed_candidates:
        normalized = normalize_candidate(config, candidate)
        if normalized in seen:
            continue
        seen.add(normalized)
        population.append(individual_type(list(normalized)))

    while len(population) < config.ga.candidate_population_size:
        candidate = normalize_candidate(
            config,
            (
                rng.randint(config.balance.unit_max_hp_min, config.balance.unit_max_hp_max),
                rng.randint(config.balance.unit_max_mana_points_min, config.balance.unit_max_mana_points_max),
                rng.randint(config.balance.unit_move_points_min, config.balance.unit_move_points_max),
                rng.randint(config.balance.physical_damage_received_percent_min, config.balance.physical_damage_received_percent_max),
                rng.randint(config.balance.magic_damage_received_percent_min, config.balance.magic_damage_received_percent_max),
            ),
        )
        population.append(individual_type(list(candidate)))
    return population[: config.ga.candidate_population_size]


def mutate_candidate(individual: list[int], config: config_models.PrimaryRoleBalancerConfig, rng: random.Random):
    ranges = [
        (config.balance.unit_max_hp_min, config.balance.unit_max_hp_max, 18),
        (config.balance.unit_max_mana_points_min, config.balance.unit_max_mana_points_max, 8),
        (config.balance.unit_move_points_min, config.balance.unit_move_points_max, 1),
        (config.balance.physical_damage_received_percent_min, config.balance.physical_damage_received_percent_max, 10),
        (config.balance.magic_damage_received_percent_min, config.balance.magic_damage_received_percent_max, 10),
    ]
    for index, (minimum, maximum, step) in enumerate(ranges):
        if rng.random() < 0.6:
            individual[index] = ga.bounded_integer(individual[index] + rng.randint(-step, step), minimum, maximum)
        elif rng.random() < 0.3:
            individual[index] = rng.randint(minimum, maximum)
    return (individual,)


def run_primary_role_ga(
    config: config_models.PrimaryRoleBalancerConfig,
    candidate_evaluator: Callable[[tuple[int, int, int, int, int]], measurements.PrimaryRoleMeasurement],
    initial_candidate: tuple[int, int, int, int, int],
) -> measurements.PrimaryRoleMeasurement:
    from deap import base, creator, tools

    if not hasattr(creator, "PrimaryRoleFitnessMax"):
        creator.create("PrimaryRoleFitnessMax", base.Fitness, weights=(1.0,))
    if not hasattr(creator, "PrimaryRoleIndividual"):
        creator.create("PrimaryRoleIndividual", list, fitness=creator.PrimaryRoleFitnessMax)

    rng = random.Random(config.ga.ga_random_seed)
    toolbox = base.Toolbox()
    toolbox.register("select", tools.selTournament, tournsize=3)
    toolbox.register("mutate", mutate_candidate, config=config, rng=rng)

    measurement_cache: dict[tuple[int, int, int, int, int], measurements.PrimaryRoleMeasurement] = {}
    hall_of_fame = tools.HallOfFame(1)

    def evaluate_individual(individual: list[int]) -> tuple[float]:
        normalized = normalize_candidate(config, tuple(int(value) for value in individual))
        for index, value in enumerate(normalized):
            individual[index] = value
        if normalized not in measurement_cache:
            measurement_cache[normalized] = candidate_evaluator(normalized)
        measurement = measurement_cache[normalized]
        print_candidate_measurement(config, measurement)
        return (measurement.fitness,)

    toolbox.register("evaluate", evaluate_individual)

    population = build_initial_population(config, initial_candidate, rng, creator.PrimaryRoleIndividual)
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

        best_key = tuple(int(value) for value in hall_of_fame[0])
        print_generation_best(generation, measurement_cache[best_key])

    best_key = tuple(int(value) for value in hall_of_fame[0])
    return measurement_cache[best_key]


def print_candidate_measurement(config: config_models.PrimaryRoleBalancerConfig, measurement: measurements.PrimaryRoleMeasurement) -> None:
    print(
        "candidate "
        f"role={config.balance.target_primary_role} "
        f"hp={measurement.unit_max_hp} mana={measurement.unit_max_mana_points} move={measurement.unit_move_points} "
        f"physDR={measurement.physical_damage_received_percent} magicDR={measurement.magic_damage_received_percent} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={measurement.average_attacker_turn_count:.2f} "
        f"primary-role-score={measurement.primary_role_alignment_score:.4f} "
        f"fitness={measurement.fitness:.4f}",
        flush=True,
    )


def print_generation_best(generation: int, measurement: measurements.PrimaryRoleMeasurement) -> None:
    print(
        "generation "
        f"{generation} best "
        f"hp={measurement.unit_max_hp} mana={measurement.unit_max_mana_points} move={measurement.unit_move_points} "
        f"physDR={measurement.physical_damage_received_percent} magicDR={measurement.magic_damage_received_percent} "
        f"turn-limit-rate={measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={measurement.average_attacker_turn_count:.2f} "
        f"primary-role-score={measurement.primary_role_alignment_score:.4f} "
        f"fitness={measurement.fitness:.4f}",
        flush=True,
    )


def optimize_primary_role(
    config: config_models.PrimaryRoleBalancerConfig,
    content_path: Path,
    eval_config: eval_api.EvalCommandConfig,
    offensive_ability_ids: set[str],
) -> measurements.PrimaryRoleMeasurement:
    validate_config(config)
    initial_candidate = load_initial_candidate(config, content_path)
    candidate_evaluator = PrimaryRoleCandidateEvaluator(config, content_path, eval_config, offensive_ability_ids)

    best_measurement = run_primary_role_ga(config, candidate_evaluator.evaluate, initial_candidate)
    # Candidate evaluation edits the working content pack each time, so leave it on the selected result.
    apply_candidate_to_content(
        config,
        content_path,
        (
            best_measurement.unit_max_hp,
            best_measurement.unit_max_mana_points,
            best_measurement.unit_move_points,
            best_measurement.physical_damage_received_percent,
            best_measurement.magic_damage_received_percent,
        ),
    )
    return best_measurement


def run(config: config_models.PrimaryRoleBalancerConfig) -> int:
    runtime.ensure_local_deap()

    # Work on generated content so repeated GA runs do not mutate the checked-in seed content.
    validate_config(config)
    content_path = prepare_eval_content(config)
    eval_config = build_eval_config(config, content_path)
    offensive_ability_ids = scenarios.load_offensive_ability_ids(content_path)
    best_measurement = optimize_primary_role(config, content_path, eval_config, offensive_ability_ids)

    print(
        "best "
        f"role={config.balance.target_primary_role} "
        f"hp={best_measurement.unit_max_hp} mana={best_measurement.unit_max_mana_points} move={best_measurement.unit_move_points} "
        f"physDR={best_measurement.physical_damage_received_percent} magicDR={best_measurement.magic_damage_received_percent} "
        f"turn-limit-rate={best_measurement.turn_limit_rate:.2%} "
        f"avg-attacker-turns={best_measurement.average_attacker_turn_count:.2f} "
        f"primary-role-score={best_measurement.primary_role_alignment_score:.4f} "
        f"fitness={best_measurement.fitness:.4f}",
        flush=True,
    )
    print(f"turn-count={config.ga.evaluation_turn_budget}", flush=True)
    print(f"content={content_path}", flush=True)
    return 0


def main() -> int:
    return run(load_balancer_config(parse_args()))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Tune unit template stats for one primary role.")
    add_config_arguments(parser)
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
