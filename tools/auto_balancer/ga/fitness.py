from __future__ import annotations

from math import sqrt


def compute_target_band_fitness(
    observed_value: float,
    target_low: float,
    target_high: float,
) -> float:
    target_center = (target_low + target_high) / 2.0
    distance_to_center = abs(observed_value - target_center)

    if target_low <= observed_value <= target_high:
        return 1.0 - distance_to_center

    distance_to_band = min(abs(observed_value - target_low), abs(observed_value - target_high))
    return 1.0 - distance_to_center - (distance_to_band * 4.0)


def compute_wilson_interval(
    wins: int,
    total_runs: int,
    z_score: float = 1.96,
) -> tuple[float, float]:
    if total_runs <= 0:
        return 0.0, 1.0

    observed_value = wins / total_runs
    z_squared = z_score * z_score
    denominator = 1.0 + (z_squared / total_runs)
    center = observed_value + (z_squared / (2.0 * total_runs))
    margin = z_score * sqrt(
        ((observed_value * (1.0 - observed_value)) / total_runs) + (z_squared / (4.0 * total_runs * total_runs))
    )

    lower = (center - margin) / denominator
    upper = (center + margin) / denominator
    return max(0.0, lower), min(1.0, upper)


def interval_overlaps_target_band(
    interval_low: float,
    interval_high: float,
    target_low: float,
    target_high: float,
) -> bool:
    return not (interval_high < target_low or interval_low > target_high)


def compute_confidence_adjusted_target_band_fitness(
    observed_value: float,
    interval_low: float,
    interval_high: float,
    target_low: float,
    target_high: float,
) -> float:
    raw_fitness = compute_target_band_fitness(observed_value, target_low, target_high)
    uncertainty_penalty = (interval_high - interval_low) / 2.0
    return raw_fitness - uncertainty_penalty

