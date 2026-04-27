from __future__ import annotations

import random
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, TypeVar

import auto_balancer.eval as eval_api
import auto_balancer.ga as ga
import auto_balancer.runtime as runtime
import auto_balancer.scenarios as scenarios
from auto_balancer.workflows.candidate import CandidateWorkflow, run_candidate_workflow
from balancing_scripts.primary_roles.common import mean


StatCandidate = tuple[int, int, int, int, int]
MeasurementT = TypeVar("MeasurementT")


@dataclass(frozen=True)
class StatBounds:
    hp: tuple[int, int]
    mana: tuple[int, int]
    move: tuple[int, int]
    phys_dr: tuple[int, int]
    magic_dr: tuple[int, int]


def prepare_eval_content_from_config(config: object, source_content_path: Path | None = None) -> Path:
    content_source = runtime.DEFAULT_GA_CONTENT_DIR if source_content_path is None else source_content_path
    scenario_config = scenarios.ScenarioGenerationConfig(
        seed=config.scenario.scenario_generation_random_seed,
        generated_scenarios_per_run=config.scenario.generated_scenario_count,
        map_width=config.scenario.map_width_tiles,
        map_height=config.scenario.map_height_tiles,
    )
    generated_content_path = scenarios.build_generated_content_path(
        content_source,
        config.scenario.scenario_generation_random_seed,
        config.scenario.generated_scenario_count,
    )
    return scenarios.prepare_generated_content(
        source_content_path=content_source,
        generated_content_path=generated_content_path,
        config=scenario_config,
    )


def build_role_alignment_eval_config(config: object, content_path: Path) -> eval_api.EvalCommandConfig:
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


def load_initial_stat_candidate(
    content_path: Path,
    primary_role: str | None,
    secondary_role: str | None,
) -> StatCandidate:
    unit_templates = scenarios.load_unit_templates(content_path)
    matched_units = [
        unit_template
        for unit_template in unit_templates
        if (primary_role is None or unit_template.get("primaryRole") == primary_role)
        and (secondary_role is None or unit_template.get("secondaryRole") == secondary_role)
    ]
    if not matched_units:
        raise ValueError(
            "No units matched "
            f"primaryRole={primary_role!r}, secondaryRole={secondary_role!r}."
        )

    return (
        round(mean(int(unit["maxHP"]) for unit in matched_units)),
        round(mean(int(unit["maxManaPoints"]) for unit in matched_units)),
        round(mean(int(unit["movePoints"]) for unit in matched_units)),
        round(mean(int(unit["physicalDamageReceived"]) for unit in matched_units)),
        round(mean(int(unit["magicDamageReceived"]) for unit in matched_units)),
    )


def build_field_values(candidate: StatCandidate) -> dict[str, int]:
    unit_max_hp, unit_max_mana_points, unit_move_points, physical_damage_received_percent, magic_damage_received_percent = candidate
    return {
        "maxHP": unit_max_hp,
        "maxManaPoints": unit_max_mana_points,
        "movePoints": unit_move_points,
        "physicalDamageReceived": physical_damage_received_percent,
        "magicDamageReceived": magic_damage_received_percent,
    }


def apply_candidate_to_content(
    content_path: Path,
    primary_role: str | None,
    secondary_role: str | None,
    candidate: StatCandidate,
) -> None:
    scenarios.update_unit_templates_for_role(
        content_path,
        primary_role,
        secondary_role,
        build_field_values(candidate),
    )


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


def pct_bounds_stat(initial: int, max_pct: float, floor: int) -> tuple[int, int]:
    lo = max(floor, round(initial * (1.0 - max_pct)))
    hi = round(initial * (1.0 + max_pct))
    if lo > hi:
        lo = hi
    return lo, hi


def normalize_candidate(bounds: StatBounds, candidate: StatCandidate) -> StatCandidate:
    hp, mana, move, phys_dr, magic_dr = candidate
    return (
        ga.bounded_integer(hp, *bounds.hp),
        ga.bounded_integer(mana, *bounds.mana),
        ga.bounded_integer(move, *bounds.move),
        ga.bounded_integer(phys_dr, *bounds.phys_dr),
        ga.bounded_integer(magic_dr, *bounds.magic_dr),
    )


def build_initial_population(
    population_size: int,
    bounds: StatBounds,
    initial_candidate: StatCandidate,
    seed_candidates: list[StatCandidate],
    rng: random.Random,
    individual_type: type,
) -> list:
    population: list = []
    seen: set[StatCandidate] = set()
    for candidate in [initial_candidate, *seed_candidates]:
        normalized = normalize_candidate(bounds, candidate)
        if normalized in seen:
            continue
        seen.add(normalized)
        population.append(individual_type(list(normalized)))

    while len(population) < population_size:
        candidate = normalize_candidate(
            bounds,
            (
                rng.randint(*bounds.hp),
                rng.randint(*bounds.mana),
                rng.randint(*bounds.move),
                rng.randint(*bounds.phys_dr),
                rng.randint(*bounds.magic_dr),
            ),
        )
        population.append(individual_type(list(candidate)))
    return population[:population_size]


def build_default_step_sizes(bounds: StatBounds) -> list[int]:
    return [
        max(1, (bounds.hp[1] - bounds.hp[0]) // 8),
        max(1, (bounds.mana[1] - bounds.mana[0]) // 8),
        max(1, (bounds.move[1] - bounds.move[0]) // 2),
        max(1, (bounds.phys_dr[1] - bounds.phys_dr[0]) // 8),
        max(1, (bounds.magic_dr[1] - bounds.magic_dr[0]) // 8),
    ]


def mutate_candidate(
    individual: list[int],
    bounds: StatBounds,
    rng: random.Random,
    step_sizes: list[int] | None = None,
):
    stat_bounds = [bounds.hp, bounds.mana, bounds.move, bounds.phys_dr, bounds.magic_dr]
    active_step_sizes = build_default_step_sizes(bounds) if step_sizes is None else step_sizes
    for index, ((lo, hi), step) in enumerate(zip(stat_bounds, active_step_sizes)):
        if rng.random() < 0.6:
            individual[index] = ga.bounded_integer(individual[index] + rng.randint(-step, step), lo, hi)
        elif rng.random() < 0.3:
            individual[index] = rng.randint(lo, hi)
    return (individual,)


class StatCandidateWorkflow(CandidateWorkflow[StatCandidate, MeasurementT]):
    def __init__(
        self,
        *,
        creator_name_prefix: str,
        random_seed: int,
        population_size: int,
        generation_count: int,
        mutation_probability: float,
        bounds: StatBounds,
        initial_candidate: StatCandidate,
        seed_candidates: list[StatCandidate],
        mutation_step_sizes: list[int] | None,
        candidate_evaluator: Callable[[StatCandidate], MeasurementT],
        get_fitness: Callable[[MeasurementT], float],
        on_candidate: Callable[[MeasurementT], None],
        on_generation_best: Callable[[int, MeasurementT], None],
    ):
        self.creator_name_prefix = creator_name_prefix
        self.random_seed = random_seed
        self.population_size = population_size
        self.generation_count = generation_count
        self.mutation_probability = mutation_probability
        self.bounds = bounds
        self.initial_candidate = initial_candidate
        self.seed_candidates = seed_candidates
        self.mutation_step_sizes = mutation_step_sizes
        self._candidate_evaluator = candidate_evaluator
        self._get_fitness = get_fitness
        self._on_candidate = on_candidate
        self._on_generation_best = on_generation_best

    def normalize_individual(self, individual: list[int]) -> StatCandidate:
        return normalize_candidate(self.bounds, tuple(int(value) for value in individual))

    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        return build_initial_population(
            self.population_size,
            self.bounds,
            self.initial_candidate,
            self.seed_candidates,
            rng,
            individual_type,
        )

    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        return mutate_candidate(individual, self.bounds, rng, self.mutation_step_sizes)

    def evaluate_candidate(self, candidate: StatCandidate) -> MeasurementT:
        return self._candidate_evaluator(candidate)

    def get_fitness(self, measurement: MeasurementT) -> float:
        return self._get_fitness(measurement)

    def on_candidate(self, measurement: MeasurementT) -> None:
        self._on_candidate(measurement)

    def on_generation_best(self, generation: int, measurement: MeasurementT) -> None:
        self._on_generation_best(generation, measurement)


def run_stat_ga(
    *,
    creator_name_prefix: str,
    random_seed: int,
    population_size: int,
    generation_count: int,
    mutation_probability: float,
    bounds: StatBounds,
    initial_candidate: StatCandidate,
    seed_candidates: list[StatCandidate],
    mutation_step_sizes: list[int] | None = None,
    candidate_evaluator: Callable[[StatCandidate], MeasurementT],
    get_fitness: Callable[[MeasurementT], float],
    on_candidate: Callable[[MeasurementT], None],
    on_generation_best: Callable[[int, MeasurementT], None],
) -> MeasurementT:
    workflow = StatCandidateWorkflow(
        creator_name_prefix=creator_name_prefix,
        random_seed=random_seed,
        population_size=population_size,
        generation_count=generation_count,
        mutation_probability=mutation_probability,
        bounds=bounds,
        initial_candidate=initial_candidate,
        seed_candidates=seed_candidates,
        mutation_step_sizes=mutation_step_sizes,
        candidate_evaluator=candidate_evaluator,
        get_fitness=get_fitness,
        on_candidate=on_candidate,
        on_generation_best=on_generation_best,
    )
    _, best_measurement = run_candidate_workflow(workflow)
    return best_measurement
