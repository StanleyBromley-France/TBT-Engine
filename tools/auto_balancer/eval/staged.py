from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Iterable, TypeVar


StageResultT = TypeVar("StageResultT")


@dataclass(frozen=True)
class RepeatStage:
    total_repeats: int


@dataclass(frozen=True)
class RepeatScheduleResult:
    wins: int
    turn_limit_count: int
    total_runs: int

    @property
    def win_rate(self) -> float:
        if self.total_runs <= 0:
            return 0.0
        return self.wins / self.total_runs

    @property
    def turn_limit_rate(self) -> float:
        if self.total_runs <= 0:
            return 0.0
        return self.turn_limit_count / self.total_runs


def validate_repeat_stages(stages: Iterable[RepeatStage]) -> None:
    stage_list = list(stages)
    if not stage_list:
        raise ValueError("Repeat stages must contain at least one stage.")

    previous_total_repeats = 0
    for stage in stage_list:
        if stage.total_repeats <= 0:
            raise ValueError("Repeat stage total_repeats must be positive.")
        if stage.total_repeats <= previous_total_repeats:
            raise ValueError("Repeat stage total_repeats must increase strictly from stage to stage.")
        previous_total_repeats = stage.total_repeats


def run_staged_repeat_schedule(
    stages: Iterable[RepeatStage],
    evaluate_batch: Callable[[int], tuple[int, int, int]],
    should_promote: Callable[[RepeatScheduleResult], bool] | None = None,
) -> RepeatScheduleResult:
    stage_list = list(stages)
    validate_repeat_stages(stage_list)

    completed_repeats = 0
    total_wins = 0
    total_turn_limit_count = 0
    total_runs = 0

    for index, stage in enumerate(stage_list):
        additional_repeats = stage.total_repeats - completed_repeats
        if additional_repeats > 0:
            batch_wins, batch_turn_limit_count, batch_runs = evaluate_batch(additional_repeats)
            completed_repeats += additional_repeats
            total_wins += batch_wins
            total_turn_limit_count += batch_turn_limit_count
            total_runs += batch_runs

        is_last_stage = index == len(stage_list) - 1
        if is_last_stage:
            break

        repeat_result = RepeatScheduleResult(
            wins=total_wins,
            turn_limit_count=total_turn_limit_count,
            total_runs=total_runs,
        )
        if should_promote is not None and not should_promote(repeat_result):
            break

    return RepeatScheduleResult(
        wins=total_wins,
        turn_limit_count=total_turn_limit_count,
        total_runs=total_runs,
    )


def run_staged_total_repeat_schedule(
    stages: Iterable[RepeatStage],
    evaluate_stage: Callable[[int], StageResultT],
    should_promote: Callable[[StageResultT], bool] | None = None,
) -> StageResultT:
    stage_list = list(stages)
    validate_repeat_stages(stage_list)

    stage_result: StageResultT | None = None
    for index, stage in enumerate(stage_list):
        stage_result = evaluate_stage(stage.total_repeats)

        is_last_stage = index == len(stage_list) - 1
        if is_last_stage:
            break
        if should_promote is not None and not should_promote(stage_result):
            break

    if stage_result is None:
        raise ValueError("Repeat stages must produce at least one result.")

    return stage_result
