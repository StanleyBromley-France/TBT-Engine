# Simulation-Based Automated Balancing in Turn-Based Tactical Games

## Supervisor

Dr Lauren Ansell

## Project Vision

This project investigates how automated simulations and genetic algorithms can support balance decisions in turn-based tactical games. Manual playtesting is often slow, subjective, and difficult to repeat consistently, especially for small teams or solo developers. This project does not aim to replace large-scale user testing, which remains an important source of feedback on game balance. Instead, it explores whether a genetic algorithm can serve as a first-pass balance check, identifying obvious balance problems early, allowing faster iteration, and giving indie developers a more accessible way to evaluate tactical game content without requiring large testing resources.

## Project Aim

The aim of this project is to design, implement, and evaluate a simulation-based balancing pipeline for a turn-based tactical combat system. The project combines a deterministic turn-based tactics simulation engine, AI-controlled automated matches, and a genetic algorithm that searches for improved content configurations based on simulation results.

## Repository Scope

This repository is intended as an open-source example of how a simulation-based genetic balancing pipeline for turn-based tactics games can be built and evaluated, rather than as a general-purpose balancing tool. It can be used as a balancing tool for the included tactics system, but adapting it to other games would require changes to the combat model, content schema, scenario generation, evaluation metrics, and optimisation objectives.

## TBT Engine

In this repository, "TBT engine" refers to the complete simulation stack: the core turn-based tactics rules engine, the scenario setup pipeline, the simulation runner, and the AI agents used to execute automated matches.

The TBT Engine is a simulation-first turn-based tactics engine built to support automated balance testing of given JSON content in a compatible schema. It features hex-grid combat, unit roles, abilities, effects, turn flow, a deterministic game state, and MCTS-driven matches, allowing content to be tested repeatedly rather than by hand. Its focus is iteration and data reporting, not a playable game. It supports creating reproducible scenarios, running them with controlled seeds and agent profiles, and producing results that guide changes to game content.

## Gameplay Rules

The simulated game is a two-team tactical combat system played on a hex grid. Matches are loaded from JSON game states, then automated agents choose legal actions until one side wins or the configured turn limit is reached. The rules cover unit resources, movement, ability targeting, effects, action commitment, turn flow, and victory conditions.

The no-move content pack uses the same engine rules, but disables movement as a balance variable by giving units `0` move points and using configuration that keeps movement-related genome values fixed. For the detailed rule reference, see [Gameplay Rules](docs/gameplay-rules.md).

## MCTS Agent

Automated matches are played by Monte Carlo Tree Search agents. The agents explores legal action sequences, simulates possible outcomes, and uses a weighted state evaluator to prefer useful combat behaviour such as winning, preserving allied units, damaging enemies, managing HP and mana, and benefiting from buffs or debuffs.

The MCTS weights define the behaviour promoted during simulation, which matters because the balancer evaluates content through these automated matches. For the full explanation of the scoring terms and promoted behaviour, see [MCTS Weights And Promoted Behaviour](docs/mcts-weights.md).

## Full-Genome Balancer

The full-genome balancer uses the TBT Engine as its simulation base through the CLI entry point. It evaluates candidate content configurations with automated matches, scores them against balance targets, and uses a genetic algorithm to search for improved JSON content.

The main supported workflow is the full-genome balancer, including the no-move variant used for movement-disabled experiments. For the detailed workflow, see [Full Genome Balancer Explanation](docs/full-genome-balancer.md).

## Staged Balancer (Outdated)

The staged balancer was an earlier approach to automated balancing that predates the full-genome balancer. It is no longer the active workflow but remains in the repository as part of the development history. For the detailed explanation and review, see [Staged balancer (Outdated)](docs/pipeline-balancer.md).

## Repository Structure

The repository is organised around the C# simulation engine, the JSON content it evaluates, and the Python tooling used to run automated balancing experiments. For a full breakdown of each directory and its role, see [System Design](docs/system-design.md).

## DEAP and the Genetic Algorithm

The Python balancing pipeline uses [DEAP](https://deap.readthedocs.io/) (Distributed Evolutionary Algorithms in Python) to provide the core genetic algorithm mechanics: tournament selection, two-point crossover, fitness tracking, and a hall of fame. DEAP is vendored at `tools/dependences/deap-1.4.3/` so the project is self-contained. The project wraps DEAP in its own `CandidateWorkflow` abstraction, which handles checkpointing, evaluation caching, and the generation loop. For a full breakdown of which DEAP components are used and how they fit into the balancer, see [DEAP Usage](docs/deap-usage.md).

DEAP is licenced under the GNU Lesser General Public License v3 (LGPL-3.0). Its licence is included at `tools/dependences/deap-1.4.3/LICENSE.txt`. See [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES) for the full third-party notice.

## Requirements

This project uses both C# and Python components. The C# projects provide the turn-based tactics engine, simulation runner, CLI, and tests. The Python tools provide the automated balancing pipeline.

* .NET SDK 9.0 is required to build and run the full solution, including the test projects.
* Python 3 is required for the balancing tools in tools/.
* Python package requirements are listed in tools/requirements.txt.
  * This includes numpy, which DEAP relies on.
  * The DEAP genetic algorithm dependency is vendored under tools/dependences/deap-1.4.3/.

## Installation

Clone the repository and enter the project directory:

```bash
git clone <https://github.com/StanleyBromley-France/TBT-Engine>
cd TBT-Engine
```
Restore and build the C# solution:

```bash
dotnet restore TBT-Engine.sln
dotnet build TBT-Engine.sln
```

To run the C# test suite:
```bash
dotnet test TBT-Engine.sln
```

Install the Python requirements:

This installs Numpy, which is needed for DEAP.
```bash
pip install -r tools/requirements.txt 
```

## How to Run

### Run the CLI in interactive mode:
```bash
dotnet run --project src/cli/cli.csproj
```

### Run an evaluation using the default content pack and default settings with action-by-action output:
```bash
dotnet run --project src/cli/cli.csproj -- eval --verbose
```

Useful evaluation options include:
- `--content` sets the content directory to load.
- `--game-state` selects a specific game state id. Leave it blank to evaluate all scenarios.
- `--repeat-count` sets how many repeated simulations to run.
- `--seed` sets the base evaluation seed. Each repeated run derives its run seed from this value, and the engine derives map, simulation, and MCTS search seeds from that run seed.
- `--parallelism` sets how many simulations can run in parallel.
- `--max-turns` sets the attacker turn limit.
- `--quiet` reduces console output.
- `--verbose` prints more detailed evaluation output.
- `--output` sets the evaluation result JSON path.

### Run the automated balancing pipeline:

**This balancer command can take hours to run and is CPU heavy**
```bash
py tools/run_full_genome_balancer.py
```

### Run the automated balancing pipeline no move version:

**This balancer command can take hours to run and is CPU heavy**
```bash
py tools\run_full_genome_balancer.py --ga-config tools\configs\ga\full-genome-no-move.json --balance-config tools\configs\balance\full-genome-no-move.json --scenario-config tools\configs\scenario\full-genome-no-move-scenario-eval.json --input-package content-no-move
```

### Smoke test the no-move balancer (quick check):

Use the smoke configs to verify the no-move balancer runs end-to-end without errors. This uses a minimal population and a single generation, so it completes in a few minutes rather than hours.

```bash
py tools\run_full_genome_balancer.py --ga-config tools\configs\ga\full-genome-no-move-smoke.json --balance-config tools\configs\balance\full-genome-no-move.json --scenario-config tools\configs\scenario\full-genome-no-move-smoke-scenario-eval.json --input-package content-no-move --parallelism 2
```

The smoke run uses 4 candidates, 1 generation, 4 generated scenarios, and an MCTS budget of 64. It is only for verifying the pipeline runs correctly — the results are not meaningful for balance evaluation.

For full-genome options, seed matrix usage, output packages, and resume behaviour, see [Full Genome Balancer Explanation](docs/full-genome-balancer.md).

## Results

Completed balancer runs were conducted using the no-movement content variant. The movement-enabled variant was not run to completion due to long simulation time. For the full results, including fitness charts, outcome tables, changed content diffs, and seed matrix stability evidence, see [Balancer Results: No-Movement Runs](docs/results/no-movement-runs.md).

## Documentation

- [Full Genome Balancer Explanation](docs/full-genome-balancer.md)
- [System design and UML history](docs/system-design.md)
- [Gameplay rules](docs/gameplay-rules.md)
- [MCTS weights and promoted behaviour](docs/mcts-weights.md)
- [Content schema](docs/content-schema.md)
- [Configuration files](docs/configuration.md)
- [DEAP usage](docs/deap-usage.md)
- [Balancer results: no-movement runs](docs/results/no-movement-runs.md)
- [Staged balancer (Outdated)](docs/pipeline-balancer.md)

## License

This project is released under the [Creative Commons Attribution 4.0 International (CC BY 4.0)](https://creativecommons.org/licenses/by/4.0/) licence. You are free to use, modify, and distribute it for any purpose, including commercially, as long as appropriate credit is given. See [LICENSE](LICENSE) for details.
