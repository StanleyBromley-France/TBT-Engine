# Full-Genome Configuration Files

The full-genome balancer is driven by JSON config files under `tools/configs/`. These configs decide what gets evaluated, how the genetic algorithm searches, how the values are allowed to change, and how candidate fitness is scored.

The full-genome workflow uses three config types:

- `tools/configs/scenario/`
- `tools/configs/ga/`
- `tools/configs/balance/`

The full-genome runner combines one config from each folder:

```text
scenario config + GA config + balance config
```

## Scenario Configs

Scenario configs control scenario generation and evaluation setup for full-genome candidate evaluation.

Common fields are:

- `scenario_generation_random_seed`: seed used when generating scenario sets.
- `evaluation_random_seed`: base seed used by the C# evaluation runs.
- `map_width_tiles`: generated map width.
- `map_height_tiles`: generated map height.
- `game_state_id`: specific game state id to evaluate. An empty string evaluates all generated scenarios.
- `validation_mode`: content validation mode, usually `Strict`.
- `generated_scenario_count`: number of scenarios to generate.

Example:

```json
{
  "scenario_generation_random_seed": 3422345,
  "evaluation_random_seed": 3867623,
  "map_width_tiles": 5,
  "map_height_tiles": 3,
  "game_state_id": "",
  "validation_mode": "Strict",
  "generated_scenario_count": 48
}
```

Generated scenarios are built from the current content pack. Each generated team contains one `Tank`, one `Damage`, and one `Healer`, with units cycled from the available templates. The generated scenarios are written to a generated content directory, leaving the source content unchanged unless the `--persist-results` command is used.

## GA Configs

GA configs control search behaviour and simulation budget.

Common fields are:

- `ga_random_seed`: seed used by the genetic algorithm.
- `candidate_population_size`: number of candidates in each generation.
- `generation_count`: number of generations to run.
- `mutation_probability`: probability that an offspring candidate mutates.
- `crossover_probability`: probability that paired offspring exchange genome sections.
- `evaluation_turn_budget`: attacker turn limit passed into evaluation.
- `evaluation_repeat_stages`: staged repeat schedule for evaluating candidates.
- `evaluation_timeout_seconds`: maximum time allowed for evaluation subprocesses.
- `evaluation_log_mode`: logging level for evaluation, usually `quiet`, summary-like, or verbose depending on the workflow.
- `mcts_iteration_budget`: MCTS search iterations used by the evaluation agents.

Example:

```json
{
  "ga_random_seed": 9482716,
  "candidate_population_size": 80,
  "generation_count": 10,
  "mutation_probability": 0.90,
  "crossover_probability": 0.50,
  "evaluation_turn_budget": 10,
  "evaluation_repeat_stages": [
    { "total_repeats": 3 }
  ],
  "evaluation_timeout_seconds": 18000,
  "evaluation_log_mode": "quiet",
  "mcts_iteration_budget": 64
}
```

## Balance Configs

Balance configs define how the values the balancer is allowed to change and what "good" means for a run.

Balance configs contains:

- `genome`: the ordered list of unit profile genes and ability group genes.
- `search_space`: allowed ranges for unit stat modifiers and ability group modifiers.
- `targets`: target bands for match flow, role identity, fairness, and change shape.
- `fitness_weights`: how much each scoring category contributes to total fitness.
- `floor_penalties`: penalties for candidates that fall below minimum role-identity scores.
- `mutation`: probabilities and step sizes used when mutating candidates.

In `tools/configs/balance/full-genome-no-move.json`, movement-related configuration is still visible where the shared schema requires it, but the values are fixed or disabled.


Values inside the band are considered good. Values outside the band receive lower fitness according to the scoring function used by that workflow.

Fitness weights determine trade-offs. For example, in the no-move full-genome config:

```json
"fitness_weights": {
  "match_flow": 0.5,
  "primary_role_identity": 0.19,
  "secondary_role_identity": 0.16,
  "role_profile_fairness": 0.09,
  "change_shape": 0.06
}
```

This puts the most weight on overall match flow while still preserving role identity, role-profile fairness, and change-shape constraints.

## Full-Genome Config Variants

The repository includes several full-genome config variants:

- `full-genome.json`: main full-genome run.
- `full-genome-smoke.json`: smaller/cheaper run for quick checks.
- `full-genome-matrix.json`: seed-matrix GA config.
- `full-genome-no-move.json`: run variant for content where movement-related behaviour is restricted.
- `full-genome-no-move-matrix.json`: seed-matrix variant for no-move experiments.

Use smoke configs to test that the full-genome balancer runs. Use full configs when collecting real evidence.

## No-Move Variant

The no-move variant is configured by:

- `tools/configs/ga/full-genome-no-move.json`
- `tools/configs/scenario/full-genome-no-move-scenario-eval.json`
- `tools/configs/balance/full-genome-no-move.json`
- `content-no-move/`

The following full-genome concepts are unused or fixed in the no-move variant:

- Acrobat-specific genome profiles are unused. `Tank+Acrobat`, `Healer+Acrobat`, and `Damage+Acrobat` are not included in `profile_order`.
- `move_points_additive_delta` is fixed to `[0, 0]` for every primary role.
- unit `movePoints` values in `content-no-move/` are `0`.
- `ranged_range_delta` is not included in the no-move `ability_effect_groups.group_order`.
- `ranged_range_additive_delta` is fixed to `[0, 0]`.
- movement scoring targets are disabled with `null` values:
  - `all_units_average_tiles_moved_total`
  - `non_acrobat_average_tiles_moved_total`
  - `acrobat_average_tiles_moved_total`
  - `acrobat_to_non_acrobat_move_ratio`

Other role and ability groups remain active, including tank damage, healer healing, damage output, buffer modifiers, debuffer modifiers, mana costs, match flow, primary role identity, secondary buff/debuff identity, fairness, and change-shape scoring.

## Seeds

There are several seed types:

- `ga_random_seed`: controls GA population generation, mutation, and selection randomness.
- `scenario_generation_random_seed`: controls generated scenario sets.
- `evaluation_random_seed`: controls base simulation seeds passed to the C# engine.
- custom seed-matrix seeds: run several GA/scenario seed combinations to test stability.

Changing only one seed is useful for isolating where variance comes from. Changing all seeds gives a broader robustness check.

## Runtime And Confidence Trade-Offs

The most important runtime knobs are:

- `candidate_population_size`
- `generation_count`
- `generated_scenario_count`
- `evaluation_repeat_stages`
- `mcts_iteration_budget`

Increasing these usually gives stronger evidence but can make runs much slower. 
