from __future__ import annotations

from typing import Iterable


def mean(values: Iterable[float]) -> float:
    values_list = list(values)
    if not values_list:
        return 0.0
    return sum(values_list) / len(values_list)


def safe_ratio(numerator: float, denominator: float) -> float:
    if denominator <= 0.0:
        return 0.0 if numerator <= 0.0 else numerator
    return numerator / denominator
