from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.eval.staged import RepeatStage


@dataclass(frozen=True)
class ScenarioConfig:
    scenario_generation_random_seed: int
    evaluation_random_seed: int
    map_width_tiles: int
    map_height_tiles: int
    game_state_id: str
    validation_mode: str
    generated_scenario_count: int


@dataclass(frozen=True)
class GaConfig:
    ga_random_seed: int
    candidate_population_size: int
    generation_count: int
    mutation_probability: float
    evaluation_turn_budget: int
    evaluation_repeat_stages: tuple[RepeatStage, ...]
    evaluation_timeout_seconds: int
    evaluation_log_mode: str


@dataclass(frozen=True)
class NestedPrimaryRolesGaConfig:
    ga_random_seed: int
    candidate_population_size: int
    generation_count: int
    mutation_probability: float
    evaluation_turn_budget: int
    random_seed_step_per_role: int
    random_seed_step_per_round: int
    evaluation_repeat_stages: tuple[RepeatStage, ...]
    evaluation_timeout_seconds: int
    evaluation_log_mode: str


@dataclass(frozen=True)
class NestedCombinationsGaConfig:
    ga_random_seed: int
    candidate_population_size: int
    generation_count: int
    mutation_probability: float
    evaluation_turn_budget: int
    random_seed_step_per_combination: int
    random_seed_step_per_round: int
    evaluation_repeat_stages: tuple[RepeatStage, ...]
    evaluation_timeout_seconds: int
    evaluation_log_mode: str
