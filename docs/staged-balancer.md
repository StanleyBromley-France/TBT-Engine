# Staged Balancer (Outdated)

## What It Did

The staged balancer ran balancing in three sequential stages, each targeting a narrower problem than the next:

1. **Primary role baselines** — tuned HP, mana, move points, and damage-received percentages independently for Tank, Healer, and Damage units, establishing broad stat baselines before combination tuning.
2. **Role combination stats** — tuned the same stat fields for all nine primary+secondary role combinations, refining the baselines with combination-specific adjustments.
3. **Ability effects** — tuned grouped ability multipliers (damage, healing, support modifiers, mana costs) to bring ability values in line with the role stat changes.

Each stage ran its own GA and produced its own output package, which fed into the next stage.

The entry point was `tools/run_balancer.py`, driven by a staged config such as `tools/configs/pipeline/full-pipeline.json`.

## Why It Was Replaced

The Idea of this staged approach is that it made runs shorter and less risky. But it had a fundamental limitation: each stage optimised in isolation. Changes made in the role stat stage were invisible to the ability effects stage until the next full staged run, and the fitness signals from one stage could not inform decisions in another.

The full-genome balancer addresses this by treating unit stats and ability effect values as a single genome and evaluating all objectives together in each generation. This produces better-integrated candidates and a clearer fitness signal.

The issue the staged approach was trying to fix, shortening individual runs, was overcome in a different way. Through checkpointing and no-movement runs.

## What Remains in the Repository

The following files belong to the staged balancer and are not used by the current workflow:

**Script:**
- `tools/run_balancer.py`

**Workflows:**
- `tools/auto_balancer/workflows/primary_roles.py`
- `tools/auto_balancer/workflows/combined_primary_roles.py`
- `tools/auto_balancer/workflows/secondary_roles.py`

**Stages:**
- `tools/auto_balancer/stages/primary_role_baselines.py`
- `tools/auto_balancer/stages/role_combination_stats.py`

**GA configs:**
- `tools/configs/ga/nested-primary-roles.json`
- `tools/configs/ga/nested-combinations.json`
- `tools/configs/ga/ability-effects.json`
- `tools/configs/ga/standard.json`
- `tools/configs/ga/small.json`
- `tools/configs/ga/smoke-primary-roles.json`
- `tools/configs/ga/smoke-combinations.json`
- `tools/configs/ga/smoke-ability-effects.json`

**Balance configs:**
- `tools/configs/balance/primary-role-tank.json`
- `tools/configs/balance/primary-role-healer.json`
- `tools/configs/balance/primary-role-damage.json`
- `tools/configs/balance/primary-roles-nested.json`
- `tools/configs/balance/nested-combinations.json`
- `tools/configs/balance/ability-effects.json`
- `tools/configs/balance/attacker-turn-limit.json`
- `tools/configs/balance/secondary-role-acrobat.json`
- `tools/configs/balance/secondary-role-buffer.json`
- `tools/configs/balance/secondary-role-debuffer.json`
- `tools/configs/balance/terrain-distribution.json`

**Scenario configs:**
- `tools/configs/scenario/generated-scenario-eval.json`
- `tools/configs/scenario/smoke-scenario-eval.json`

**Staged configs:**
- `tools/configs/pipeline/full-pipeline.json`
- `tools/configs/pipeline/smoke-pipeline.json`
- `tools/configs/pipeline/full-genome-pipeline.json`
