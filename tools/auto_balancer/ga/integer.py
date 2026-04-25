from __future__ import annotations

import random
from typing import Iterable


def bounded_integer(value: int, minimum: int, maximum: int) -> int:
    return max(minimum, min(maximum, int(value)))


def make_integer_initial_population(
    individual_type: type,
    population_size: int,
    minimum: int,
    maximum: int,
    initial_value: int,
    random_source: random.Random,
) -> list:
    seed_values = [minimum, maximum, bounded_integer(initial_value, minimum, maximum)]
    population = [individual_type([value]) for value in dict.fromkeys(seed_values)]

    while len(population) < population_size:
        population.append(individual_type([random_source.randint(minimum, maximum)]))

    return population[:population_size]


def mutate_integer_gene(
    individual: list[int],
    minimum: int,
    maximum: int,
    random_source: random.Random,
) -> tuple[list[int]]:
    individual[0] = random_source.randint(minimum, maximum)
    return (individual,)


def evaluate_invalid_individuals(individuals: Iterable, evaluator) -> None:
    for individual in individuals:
        if individual.fitness.valid:
            continue
        individual.fitness.values = evaluator(individual)
