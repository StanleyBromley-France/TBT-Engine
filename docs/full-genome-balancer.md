# Full Genome Balancer

The full-genome balancer is the broadest automated balancing workflow in this repository. Instead of tuning one role, ability group, or scenario parameter at a time, it searches across unit stat modifiers and ability effect modifiers together. The aim is to find a content package that improves overall match flow while preserving distinct primary and secondary role behaviour.

The workflow is implemented by `tools/run_full_genome_balancer.py` and `tools/auto_balancer/workflows/full_genome.py`.

## What "Full Genome" Means

In this project, a full-genome candidate is a list of integer genes. Each candidate represents a possible set of changes to the JSON content pack.

The genome has two main parts:

- unit profile modifier genes
- ability effect group genes

Unit profile modifier genes are applied to unit templates that match a configured role profile, such as `Tank`, `Tank+Buffer`, `Healer+Acrobat`, or `Damage+Debuffer`. In the no-move variant, Acrobat-specific profiles are not tuned separately.

Ability effect group genes are applied globally to grouped ability values, such as tank damage, healer healing, buffer modifiers, debuffer modifiers, mana costs, and ranged ability range. In the no-move variant, ranged range tuning is unused.

## Genome Layout

The standard full-genome config uses 12 unit profiles:

- `Tank`
- `Tank+Buffer`
- `Tank+Debuffer`
- `Tank+Acrobat` - unused as a separate profile in the no-move variant
- `Healer`
- `Healer+Buffer`
- `Healer+Debuffer`
- `Healer+Acrobat` - unused as a separate profile in the no-move variant
- `Damage`
- `Damage+Buffer`
- `Damage+Debuffer`
- `Damage+Acrobat` - unused as a separate profile in the no-move variant

Each unit profile has five genes:

- `max_hp_multiplier_percent`
- `max_mana_points_multiplier_percent`
- `move_points_additive_delta` - fixed to `0` in the no-move variant
- `physical_damage_received_additive_delta`
- `magic_damage_received_additive_delta`

For example, a `max_hp_multiplier_percent` value of `110` means matching units receive 110% of their original max HP. A `move_points_additive_delta` value of `1` means matching units gain one move point. In the no-move variant, move-point changes are disabled.

The standard ability group section contains these genes:

- `tank_damage_percent`
- `healer_healing_percent`
- `damage_damage_percent`
- `buffer_modifier_percent`
- `debuffer_modifier_percent`
- `mana_cost_percent`
- `ranged_range_delta` - unused in the no-move variant

Most ability genes are percentage multipliers. For example, `mana_cost_percent = 90` reduces grouped mana costs to 90% of their original value. `ranged_range_delta` is different: it adds or subtracts from ranged ability range, within the configured range floor and ceiling. The no-move variant does not search this gene.

### No-Move Variant Differences

The no-move full-genome variant uses the same broad workflow, but disables movement-related search and scoring:

- `move_points_additive_delta` is fixed to `0`.
- unit `movePoints` values in `content-no-move/` are `0`.
- `Tank+Acrobat`, `Healer+Acrobat`, and `Damage+Acrobat` are not separate genome profiles.
- `ranged_range_delta` is not part of the no-move ability genome.
- movement and Acrobat movement target bands are set to `null`.
- ranged ability ranges are authored in content but not tuned by the no-move genome.

## What The Balancer Does Not Change

The full-genome balancer tunes numeric unit stats, grouped numeric ability/effect values, mana costs, and, in the standard variant, ranged ability range. It does not rewrite the structure of the content pack.

It does not change:

- ids or names
- unit roles or ability lists
- ability categories
- ability target type
- ability line-of-sight requirements
- ability area radius
- ability-to-effect links
- effect duration, stack count, harmful flag, or component lists
- effect component type or damage type
- scenario layouts in `gameStates.json`

For the no-move variant, move-point changes and ranged range changes are also fixed/unused.

## Search Space

The search space is defined in `tools/configs/balance/full-genome.json`.

This file controls:

- which unit profiles are part of the genome
- which ability groups are part of the genome
- lower and upper bounds for each unit stat modifier
- lower and upper bounds for ability multipliers
- floors and ceilings for values such as move points, damage received percentages, and ranged ability range. In the no-move variant, move-point and ranged-range changes are fixed rather than searched.
- fitness targets and weights
- mutation behaviour

Candidate values are normalized before evaluation, so out-of-range values are clamped back into the configured search space.

## Evaluation Loop

The full-genome balancer follows this process:

1. Load the source content pack.
2. Generate evaluation scenarios from the scenario config.
3. Build an initial GA population containing one neutral candidate and several random candidates.
4. Apply each candidate to a temporary content copy.
5. Run C# engine simulations through the CLI.
6. Collect match and role-performance metrics.
7. Convert those metrics into a fitness score.
8. Select, cross over, and mutate candidates to create the next generation.
9. Repeat until the configured generation count is reached.
10. Apply the best candidate and write the output balance package.

The neutral candidate represents no change:

- unit stat multipliers start at `100`
- additive stat deltas start at `0`
- ability multipliers start at `100`
- range deltas start at `0`; this gene is unused in the no-move variant

This gives the balancer a baseline candidate to compare against during the search.

## Fitness Scoring

Each candidate is evaluated by running simulations and measuring whether the resulting behaviour falls inside configured target bands.

The final fitness score is a weighted combination of:

- match flow
- primary role identity
- secondary role identity
- role profile fairness
- change shape

Match flow looks at overall game outcomes, including attacker win rate, turn-limit rate, and average attacker turn count.

Primary role identity checks whether tanks, healers, and damage dealers behave differently in the expected ways. For example, tanks should tend to survive and absorb damage, healers should provide healing, and damage units should deal more damage than non-damage units.

Secondary role identity checks whether secondary roles produce recognisable behaviour, such as buffers providing buff uptime, debuffers providing debuff uptime, and acrobats moving more than non-acrobats. In the no-move variant, Acrobat movement scoring is disabled because movement is not part of that experiment.

Role profile fairness checks whether role combinations stay within a reasonable win-rate band and whether primary or secondary role families avoid excessive spread.

Change shape discourages overly flat or overly extreme changes by measuring the spread of ability percentage changes and unit stat modifier changes.

The weighted score can also receive floor penalties if primary or secondary role identity falls below configured minimums. This helps prevent candidates that improve broad match outcomes while damaging role behaviour too much.

## Genetic Algorithm Behaviour

The GA settings are defined in `tools/configs/ga/full-genome.json`.

The main settings are:

- `ga_random_seed`
- `candidate_population_size`
- `generation_count`
- `mutation_probability`
- `crossover_probability`
- `evaluation_turn_budget`
- `evaluation_repeat_stages`
- `evaluation_timeout_seconds`
- `evaluation_log_mode`
- `mcts_iteration_budget`

The balancer uses DEAP for the GA mechanics. Selection uses tournament selection, crossover uses two-point crossover, and mutation is controlled by the full-genome mutation settings in the balance config.

Mutation can make small bounded steps, randomly reset individual genes, or replace a whole unit profile block. This allows the search to make local improvements while still occasionally exploring larger changes.

## Running The Full-Genome Balancer

If the Python virtual environment is activated:

```bash
python tools/run_full_genome_balancer.py
```

Without activating the virtual environment first:

```bash
# Windows
py tools/run_full_genome_balancer.py

```

By default, this uses:

- `tools/configs/ga/full-genome.json`
- `tools/configs/scenario/full-genome-scenario-eval.json`
- `tools/configs/balance/full-genome.json`

Output packages are written under:

```text
scratch/balance-runs/full-genome/
```

## Useful Options

- `--ga-config` sets the full-genome genetic algorithm config file.
- `--scenario-config` sets the scenario/evaluation config file.
- `--balance-config` sets the full-genome balance target/config file.
- `--input-package` runs the balancer from an existing balance package or content package instead of the default content.
- `--output-package` sets where the generated balance package should be written.
- `--resume-package` continues from a previous full-genome output package.
- `--persist-results` writes the tuned content back to the input content directory.
- `--seed-matrix` runs the full-genome seed matrix after the main full-genome run completes.
- `--ga-seeds` sets the comma-separated GA seeds used by the seed matrix.
- `--scenario-seeds` sets the comma-separated scenario-generation seeds used by the seed matrix.
- `--matrix-output-root` sets the parent directory for seed-matrix output packages.
- `--matrix-config-root` sets the parent directory for generated seed-matrix config files.
- `--dry-run` writes generated seed-matrix configs and planned output paths without launching the seed-matrix runs.

## Output Packages

A full-genome run writes a balance package containing:

- `content/`, with the tuned JSON content files
- `diffs/`, with JSON diffs showing what changed
- `report.json`, with machine-readable before/after evidence
- `report.md`, with a human-readable summary
- `ga-checkpoint.json`, when checkpointing is available for the run

The full-genome package currently writes tuned versions of:

- `unitTemplates.json`
- `effectComponentTemplates.json`
- `abilities.json`

Other content files are copied through so the package remains usable as a complete content pack.

## Resume And Checkpointing

When an output package path is used, the balancer writes a GA checkpoint sidecar during the run and copies it into the final package as `ga-checkpoint.json`.

To continue from a previous package:

```bash
python tools/run_full_genome_balancer.py --resume-package scratch/balance-runs/full-genome/<run-folder>
```

The resumed run uses the previous checkpoint population and measurement cache, then adds the current config's generation count on top. This is useful when a run was interrupted or when more generations are needed after inspecting the first result.

## Seed Matrix

The seed matrix is used to test whether a full-genome result is stable across multiple GA seeds and scenario-generation seeds.

Run the main full-genome pass and then the seed matrix:

```bash
py tools/run_full_genome_balancer.py --seed-matrix
```

Run the seed matrix on its own, without running the main full-genome pass first:

```bash
py tools/run_full_genome_seed_matrix.py
```

Preview the seed-matrix configs and planned output paths without launching the matrix runs:

```bash
python tools/run_full_genome_seed_matrix.py --dry-run
```

The seed matrix uses separate matrix configs by default:

- `tools/configs/ga/full-genome-matrix.json`
- `tools/configs/scenario/full-genome-matrix-scenario-eval.json`

The default seed lists are:

```text
GA seeds: 6157321,6158321,6159321
Scenario seeds: 3422345,3423345,3424345
```

### Custom Seeds

Custom seed lists can be supplied with `--ga-seeds` and `--scenario-seeds`:

```bash
python tools/run_full_genome_seed_matrix.py --ga-seeds 101,102,103 --scenario-seeds 201,202,203
```

The two seed lists must contain the same number of values. The matrix pairs each scenario seed with two GA seeds in a cycle, so the example above produces these seed pairs:

```text
GA 101 + scenario 201
GA 102 + scenario 201
GA 102 + scenario 202
GA 103 + scenario 202
GA 103 + scenario 203
GA 101 + scenario 203
```

The same custom seed options can also be used when running the seed matrix after the main full-genome pass:

```bash
python tools/run_full_genome_balancer.py --seed-matrix --ga-seeds 101,102,103 --scenario-seeds 201,202,203
```

The matrix pairs each scenario seed with two GA seeds in a cycle, giving broader coverage than a single run while keeping the total run count controlled.

## Limitations

The full-genome balancer is a simulation-based first-pass balancing tool. It can identify statistical problems and suggest content changes, but it does not replace human playtesting.

The quality of the result depends on:

- the accuracy of the engine simulation
- the quality of generated scenarios
- the MCTS agent behaviour
- the chosen target bands
- the number of repeats and generations
- the search space allowed by the config

A strong candidate should therefore be treated as evidence for a balance direction, not as final proof that the content is balanced.
