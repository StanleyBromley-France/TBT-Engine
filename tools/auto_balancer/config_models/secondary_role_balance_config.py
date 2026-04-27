from __future__ import annotations

from dataclasses import dataclass

from auto_balancer.config_models.shared_config import GaConfig, ScenarioConfig


@dataclass(frozen=True)
class SecondaryRoleBalanceConfig:
    target_secondary_role: str
    target_primary_role: str | None

    # ── Percentage change limits ──────────────────────────────────────────────
    # Search window is [initial × (1 − max_pct), initial × (1 + max_pct)],
    # clamped to the safety floors below.  The authored values are the intended
    # baseline; the balancer makes percentage corrections, not rewrites.
    hp_max_pct_change: float    # applies to maxHP
    mana_max_pct_change: float  # applies to maxManaPoints
    move_max_pct_change: float  # applies to movePoints
    dr_max_pct_change: float    # applies to physicalDamageReceived / magicDamageReceived

    # ── Safety floors (absolute minimums regardless of % change) ─────────────
    hp_floor: int    # e.g. 20
    mana_floor: int  # e.g. 4
    move_floor: int  # e.g. 1
    dr_floor: int    # e.g. 50 — percent-received can't drop below this

    # ── Game outcome targets (sanity check — same for all combinations) ───────
    turn_limit_rate_target_min: float
    turn_limit_rate_target_max: float
    average_attacker_turn_count_target_min: float
    average_attacker_turn_count_target_max: float
    average_action_count_target_min: float
    average_action_count_target_max: float

    # ── Primary role value proposition targets ────────────────────────────────
    # Is this primary role fulfilling its purpose well enough to be worth picking?
    # Only the targets matching target_primary_role are scored; the others sit idle.
    #
    # Tank: survives a meaningful fraction of games and absorbs enough damage
    #       that its HP pool actually matters in practice.
    tank_survival_rate_target_min: float
    tank_survival_rate_target_max: float
    tank_average_damage_taken_target_min: float
    tank_average_damage_taken_target_max: float
    #
    # Healer: heals enough per game that ally survival is meaningfully extended.
    healer_average_healing_done_target_min: float
    healer_average_healing_done_target_max: float
    #
    # Damage: deals enough damage per game to justify their lower durability.
    damage_average_damage_dealt_target_min: float
    damage_average_damage_dealt_target_max: float

    # ── Fitness weights (must sum to 1.0) ─────────────────────────────────────
    turn_limit_rate_fitness_weight: float
    average_attacker_turn_count_fitness_weight: float
    average_action_count_fitness_weight: float
    primary_role_value_fitness_weight: float
    secondary_role_alignment_fitness_weight: float


@dataclass(frozen=True)
class SecondaryRoleBalancerConfig:
    scenario: ScenarioConfig
    ga: GaConfig
    balance: SecondaryRoleBalanceConfig
