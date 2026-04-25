from __future__ import annotations

import random
from dataclasses import dataclass
from typing import Callable, TypeVar

from auto_balancer.ga.integer import evaluate_invalid_individuals, make_integer_initial_population


MeasurementT = TypeVar("MeasurementT")


@dataclass(frozen=True)
class IntegerGaConfig:
    seed: int
    minimum: int
    maximum: int
    initial_value: int
    population_size: int
    generations: int
    mutation_probability: float


def run_integer_ga(
    config: IntegerGaConfig,
    mutate_gene,
    evaluate_candidate: Callable[[int], MeasurementT],
    get_fitness: Callable[[MeasurementT], float],
    on_candidate: Callable[[MeasurementT], None] | None = None,
    on_generation_best: Callable[[int, MeasurementT], None] | None = None,
) -> MeasurementT:
    from deap import base, creator, tools

    if not hasattr(creator, "FitnessMax"):
        creator.create("FitnessMax", base.Fitness, weights=(1.0,))
    if not hasattr(creator, "Individual"):
        creator.create("Individual", list, fitness=creator.FitnessMax)

    rng = random.Random(config.seed)
    toolbox = base.Toolbox()
    toolbox.register("select", tools.selTournament, tournsize=3)
    toolbox.register("mutate", mutate_gene, minimum=config.minimum, maximum=config.maximum, random_source=rng)

    measurement_cache: dict[int, MeasurementT] = {}
    hall_of_fame = tools.HallOfFame(1)

    def evaluate_individual(individual: list[int]) -> tuple[float]:
        candidate_value = max(config.minimum, min(config.maximum, int(individual[0])))
        individual[0] = candidate_value

        if candidate_value not in measurement_cache:
            measurement_cache[candidate_value] = evaluate_candidate(candidate_value)

        measurement = measurement_cache[candidate_value]
        if on_candidate is not None:
            on_candidate(measurement)
        return (get_fitness(measurement),)

    toolbox.register("evaluate", evaluate_individual)

    population = make_integer_initial_population(
        creator.Individual,
        config.population_size,
        config.minimum,
        config.maximum,
        config.initial_value,
        rng,
    )

    evaluate_invalid_individuals(population, toolbox.evaluate)
    hall_of_fame.update(population)

    for generation in range(1, config.generations + 1):
        offspring = list(map(toolbox.clone, toolbox.select(population, len(population))))

        for individual in offspring:
            if rng.random() <= config.mutation_probability:
                toolbox.mutate(individual)
                del individual.fitness.values

        evaluate_invalid_individuals(offspring, toolbox.evaluate)
        population[:] = offspring
        hall_of_fame.update(population)

        if on_generation_best is not None:
            generation_best = measurement_cache[hall_of_fame[0][0]]
            on_generation_best(generation, generation_best)

    best_value = hall_of_fame[0][0]
    return measurement_cache[best_value]
