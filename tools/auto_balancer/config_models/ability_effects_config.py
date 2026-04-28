from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import GaConfig, ScenarioConfig


@dataclass(frozen=True)
class AbilityEffectsBalanceConfig:
    # ── Percentage change limits ──────────────────────────────────────────────
    # Each parameter's search window is [initial × (1 − max_pct), initial × (1 + max_pct)],
    # clamped to the safety floors below. The authored values are the intended
    # baseline; the balancer makes percentage corrections, not rewrites.
    component_max_pct_change: float   # applies to damage, heal, dot, hot values
    percent_mod_max_pct_change: float # applies to PercentAttributeModifier percent fields
    flat_mod_max_pct_change: float    # applies to FlatAttributeModifier amount fields
    mana_cost_max_pct_change: float   # applies to ability manaCost fields

    # ── Safety floors (absolute minimums regardless of % change) ─────────────
    # Prevent the GA from pushing values to zero or sign-flipping modifiers.
    damage_floor: int       # e.g. 4 — no damage component drops below this
    heal_floor: int         # e.g. 4
    percent_mod_floor: int  # e.g. 5 — magnitude floor (applies to abs value)
    flat_mod_floor: int     # e.g. 1
    mana_cost_floor: int    # e.g. 2

    # ── Diversity target (percentage change spread) ───────────────────────────
    # Measures std dev of the fractional changes applied: (final − initial) / initial.
    # Rewards the GA for applying varied corrections rather than uniformly
    # boosting or nerfing everything by the same amount.
    pct_change_diversity_target_min: float
    pct_change_diversity_target_max: float

    # ── Win-rate target ──────────────────────────────────────────────────────
    attacker_win_rate_target_min: float
    attacker_win_rate_target_max: float

    # ── Per-primary-role output targets ──────────────────────────────────────
    tank_average_damage_dealt_target_min: float
    tank_average_damage_dealt_target_max: float
    healer_average_healing_done_target_min: float
    healer_average_healing_done_target_max: float
    damage_average_damage_dealt_target_min: float
    damage_average_damage_dealt_target_max: float

    # ── Per-secondary-role output targets ────────────────────────────────────
    buffer_average_buff_uptime_target_min: float
    buffer_average_buff_uptime_target_max: float
    debuffer_average_debuff_uptime_target_min: float
    debuffer_average_debuff_uptime_target_max: float

    # ── Fitness weights (must sum to 1.0) ────────────────────────────────────
    win_rate_fitness_weight: float
    primary_role_fitness_weight: float
    secondary_role_fitness_weight: float
    diversity_fitness_weight: float


@dataclass(frozen=True)
class AbilityEffectsBalancerConfig:
    scenario: ScenarioConfig
    ga: GaConfig
    balance: AbilityEffectsBalanceConfig
