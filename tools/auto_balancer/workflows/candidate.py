from __future__ import annotations

import base64
import json
import pickle
import random
import time
from abc import ABC, abstractmethod
from pathlib import Path
from typing import Generic, TypeVar

from deap import base, creator, tools

import auto_balancer.ga as ga
import auto_balancer.reporting as reporting


CandidateT = TypeVar("CandidateT", bound=tuple[int, ...])
MeasurementT = TypeVar("MeasurementT")


class CandidateWorkflow(ABC, Generic[CandidateT, MeasurementT]):
    creator_name_prefix: str
    random_seed: int
    population_size: int
    generation_count: int
    mutation_probability: float
    crossover_probability: float = 0.0
    checkpoint_path: Path | None = None
    resume_checkpoint_path: Path | None = None

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

    def on_candidate(self, measurement: MeasurementT, elapsed_seconds: float, cached: bool) -> None:
        pass

    def on_generation_best(self, generation: int, measurement: MeasurementT) -> None:
        pass

    def on_interrupted_best(self, candidate: CandidateT, measurement: MeasurementT) -> None:
        pass

    def measurement_to_checkpoint(self, measurement: MeasurementT) -> object:
        raise NotImplementedError(f"{type(self).__name__} does not support GA checkpoint measurement serialization.")

    def measurement_from_checkpoint(self, payload: object) -> MeasurementT:
        raise NotImplementedError(f"{type(self).__name__} does not support GA checkpoint measurement deserialization.")


def run_candidate_workflow(workflow: CandidateWorkflow[CandidateT, MeasurementT]) -> tuple[CandidateT, MeasurementT]:
    fitness_name = f"{workflow.creator_name_prefix}FitnessMax"
    individual_name = f"{workflow.creator_name_prefix}Individual"
    if not hasattr(creator, fitness_name):
        creator.create(fitness_name, base.Fitness, weights=(1.0,))
    if not hasattr(creator, individual_name):
        creator.create(individual_name, list, fitness=getattr(creator, fitness_name))

    random.seed(workflow.random_seed)
    rng = random.Random(workflow.random_seed)
    toolbox = base.Toolbox()
    toolbox.register("select", tools.selTournament, tournsize=3)
    toolbox.register("mate", tools.cxTwoPoint)
    toolbox.register("mutate", workflow.mutate_individual, rng=rng)

    measurement_cache: dict[CandidateT, MeasurementT] = {}
    hall_of_fame = tools.HallOfFame(1)

    def evaluate_individual(individual: list[int]) -> tuple[float]:
        normalized = workflow.normalize_individual(individual)
        for index, value in enumerate(normalized):
            individual[index] = int(value)
        cached = normalized in measurement_cache
        t0 = time.monotonic()
        if normalized not in measurement_cache:
            measurement_cache[normalized] = workflow.evaluate_candidate(normalized)
        elapsed = time.monotonic() - t0
        measurement = measurement_cache[normalized]
        workflow.on_candidate(measurement, elapsed, cached)
        return (workflow.get_fitness(measurement),)

    toolbox.register("evaluate", evaluate_individual)

    try:
        individual_type = getattr(creator, individual_name)
        resume_state = load_checkpoint(workflow, individual_type)
        if resume_state is None:
            completed_generation = 0
            population = workflow.build_initial_population(individual_type, rng)
            reporting.print_section("initial population")
            ga.evaluate_invalid_individuals(population, toolbox.evaluate)
            hall_of_fame.update(population)
            write_checkpoint(
                workflow,
                completed_generation,
                population,
                measurement_cache,
                hall_of_fame,
                rng,
            )
        else:
            completed_generation = resume_state.completed_generation
            population = resume_state.population
            measurement_cache.update(resume_state.measurement_cache)
            rng.setstate(resume_state.rng_state)
            random.setstate(resume_state.python_random_state)
            hall_of_fame.update(population)
            reporting.print_section(
                "resuming population",
                [
                    reporting.field("completed-generation", completed_generation),
                    reporting.field("cached-candidates", len(measurement_cache)),
                ],
            )

        final_generation = completed_generation + workflow.generation_count
        for generation in range(completed_generation + 1, final_generation + 1):
            reporting.print_section(f"generation {generation}")
            offspring = list(map(toolbox.clone, toolbox.select(population, len(population))))
            crossover_probability = getattr(workflow, "crossover_probability", 0.0)
            if crossover_probability > 0.0:
                for child1, child2 in zip(offspring[::2], offspring[1::2]):
                    if rng.random() <= crossover_probability:
                        toolbox.mate(child1, child2)
                        del child1.fitness.values
                        del child2.fitness.values

            for individual in offspring:
                if rng.random() <= workflow.mutation_probability:
                    toolbox.mutate(individual)
                    del individual.fitness.values

            ga.evaluate_invalid_individuals(offspring, toolbox.evaluate)
            population[:] = offspring
            hall_of_fame.update(population)

            best_key = workflow.normalize_individual(hall_of_fame[0])
            workflow.on_generation_best(generation, measurement_cache[best_key])
            write_checkpoint(
                workflow,
                generation,
                population,
                measurement_cache,
                hall_of_fame,
                rng,
            )
    except KeyboardInterrupt:
        if not measurement_cache:
            raise
        best_key, best_measurement = get_best_cached_measurement(workflow, measurement_cache)
        workflow.on_interrupted_best(best_key, best_measurement)
        return best_key, best_measurement

    best_key = workflow.normalize_individual(hall_of_fame[0])
    return best_key, measurement_cache[best_key]


class CandidateWorkflowCheckpoint(Generic[CandidateT, MeasurementT]):
    def __init__(
        self,
        completed_generation: int,
        population: list,
        measurement_cache: dict[CandidateT, MeasurementT],
        rng_state: object,
        python_random_state: object,
    ):
        self.completed_generation = completed_generation
        self.population = population
        self.measurement_cache = measurement_cache
        self.rng_state = rng_state
        self.python_random_state = python_random_state


def write_checkpoint(
    workflow: CandidateWorkflow[CandidateT, MeasurementT],
    completed_generation: int,
    population: list,
    measurement_cache: dict[CandidateT, MeasurementT],
    hall_of_fame,
    rng: random.Random,
) -> None:
    if workflow.checkpoint_path is None:
        return

    best = list(hall_of_fame[0]) if len(hall_of_fame) else None
    payload = {
        "version": 1,
        "workflow": workflow.creator_name_prefix,
        "completedGeneration": completed_generation,
        "population": [list(individual) for individual in population],
        "best": best,
        "rngState": encode_pickle(rng.getstate()),
        "pythonRandomState": encode_pickle(random.getstate()),
        "measurementCache": [
            {
                "candidate": list(candidate),
                "measurement": workflow.measurement_to_checkpoint(measurement),
            }
            for candidate, measurement in measurement_cache.items()
        ],
    }

    workflow.checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
    with workflow.checkpoint_path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")


def load_checkpoint(
    workflow: CandidateWorkflow[CandidateT, MeasurementT],
    individual_type: type,
) -> CandidateWorkflowCheckpoint[CandidateT, MeasurementT] | None:
    if workflow.resume_checkpoint_path is None:
        return None

    with workflow.resume_checkpoint_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    if payload.get("workflow") != workflow.creator_name_prefix:
        raise ValueError(
            "Checkpoint workflow did not match current workflow: "
            f"{payload.get('workflow')!r} != {workflow.creator_name_prefix!r}"
        )

    measurement_cache: dict[CandidateT, MeasurementT] = {}
    for item in payload.get("measurementCache", []):
        candidate = workflow.normalize_individual(item["candidate"])
        measurement_cache[candidate] = workflow.measurement_from_checkpoint(item["measurement"])

    population = []
    for values in payload.get("population", []):
        individual = individual_type(list(workflow.normalize_individual(values)))
        candidate = workflow.normalize_individual(individual)
        if candidate in measurement_cache:
            individual.fitness.values = (workflow.get_fitness(measurement_cache[candidate]),)
        population.append(individual)

    if not population:
        raise ValueError(f"Checkpoint did not contain a population: {workflow.resume_checkpoint_path}")

    return CandidateWorkflowCheckpoint(
        completed_generation=int(payload["completedGeneration"]),
        population=population,
        measurement_cache=measurement_cache,
        rng_state=decode_pickle(payload["rngState"]),
        python_random_state=decode_pickle(payload["pythonRandomState"]),
    )


def encode_pickle(value: object) -> str:
    return base64.b64encode(pickle.dumps(value)).decode("ascii")


def decode_pickle(value: str) -> object:
    return pickle.loads(base64.b64decode(value.encode("ascii")))


def get_best_cached_measurement(
    workflow: CandidateWorkflow[CandidateT, MeasurementT],
    measurement_cache: dict[CandidateT, MeasurementT],
) -> tuple[CandidateT, MeasurementT]:
    return max(
        measurement_cache.items(),
        key=lambda item: workflow.get_fitness(item[1]),
    )
