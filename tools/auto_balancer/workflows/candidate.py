from __future__ import annotations

import random
from abc import ABC, abstractmethod
from typing import Generic, TypeVar

import auto_balancer.ga as ga


CandidateT = TypeVar("CandidateT", bound=tuple[int, ...])
MeasurementT = TypeVar("MeasurementT")


class CandidateWorkflow(ABC, Generic[CandidateT, MeasurementT]):
    creator_name_prefix: str
    random_seed: int
    population_size: int
    generation_count: int
    mutation_probability: float

    @abstractmethod
    def normalize_individual(self, individual: list[int]) -> CandidateT:
        raise NotImplementedError

    @abstractmethod
    def build_initial_population(self, individual_type: type, rng: random.Random) -> list:
        raise NotImplementedError

    @abstractmethod
    def mutate_individual(self, individual: list[int], rng: random.Random) -> tuple[list[int]]:
        raise NotImplementedError

    @abstractmethod
    def evaluate_candidate(self, candidate: CandidateT) -> MeasurementT:
        raise NotImplementedError

    @abstractmethod
    def get_fitness(self, measurement: MeasurementT) -> float:
        raise NotImplementedError

    def on_candidate(self, measurement: MeasurementT) -> None:
        pass

    def on_generation_best(self, generation: int, measurement: MeasurementT) -> None:
        pass


def run_candidate_workflow(workflow: CandidateWorkflow[CandidateT, MeasurementT]) -> tuple[CandidateT, MeasurementT]:
    from deap import base, creator, tools

    fitness_name = f"{workflow.creator_name_prefix}FitnessMax"
    individual_name = f"{workflow.creator_name_prefix}Individual"
    if not hasattr(creator, fitness_name):
        creator.create(fitness_name, base.Fitness, weights=(1.0,))
    if not hasattr(creator, individual_name):
        creator.create(individual_name, list, fitness=getattr(creator, fitness_name))

    rng = random.Random(workflow.random_seed)
    toolbox = base.Toolbox()
    toolbox.register("select", tools.selTournament, tournsize=3)
    toolbox.register("mutate", workflow.mutate_individual, rng=rng)

    measurement_cache: dict[CandidateT, MeasurementT] = {}
    hall_of_fame = tools.HallOfFame(1)

    def evaluate_individual(individual: list[int]) -> tuple[float]:
        normalized = workflow.normalize_individual(individual)
        for index, value in enumerate(normalized):
            individual[index] = int(value)
        if normalized not in measurement_cache:
            measurement_cache[normalized] = workflow.evaluate_candidate(normalized)
        measurement = measurement_cache[normalized]
        workflow.on_candidate(measurement)
        return (workflow.get_fitness(measurement),)

    toolbox.register("evaluate", evaluate_individual)

    individual_type = getattr(creator, individual_name)
    population = workflow.build_initial_population(individual_type, rng)
    ga.evaluate_invalid_individuals(population, toolbox.evaluate)
    hall_of_fame.update(population)

    for generation in range(1, workflow.generation_count + 1):
        offspring = list(map(toolbox.clone, toolbox.select(population, len(population))))
        for individual in offspring:
            if rng.random() <= workflow.mutation_probability:
                toolbox.mutate(individual)
                del individual.fitness.values

        ga.evaluate_invalid_individuals(offspring, toolbox.evaluate)
        population[:] = offspring
        hall_of_fame.update(population)

        best_key = workflow.normalize_individual(hall_of_fame[0])
        workflow.on_generation_best(generation, measurement_cache[best_key])

    best_key = workflow.normalize_individual(hall_of_fame[0])
    return best_key, measurement_cache[best_key]
