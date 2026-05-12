# DEAP Usage in This Project

## What DEAP Is

[DEAP](https://deap.readthedocs.io/) (Distributed Evolutionary Algorithms in Python) is an open-source evolutionary computation framework. This project uses it to provide the core genetic algorithm mechanics: selection, crossover, mutation bookkeeping, and a hall of fame.

DEAP is vendored at `tools/dependences/deap-1.4.3/`. The vendored copy is used so the project remains self-contained.

## Where DEAP Is Used

DEAP is used in two places:

- `tools/auto_balancer/ga/deap_runner.py` — a legacy single-integer GA runner used by older (now outdated) workflows.
- `tools/auto_balancer/workflows/candidate.py` — the `run_candidate_workflow` function that drives the full-genome balancer.

## Which DEAP Components Are Used

| Component | Purpose |
|---|---|
| `creator.create("…FitnessMax", base.Fitness, weights=(1.0,))` | Defines a single-objective maximisation fitness type |
| `creator.create("…Individual", list, fitness=…FitnessMax)` | Defines an individual as a plain Python list carrying a fitness value |
| `base.Toolbox` | Registers and dispatches GA operators (select, mate, mutate, evaluate) |
| `tools.selTournament(tournsize=3)` | Tournament selection with a pool of 3 to choose the next generation |
| `tools.cxTwoPoint` | Two-point crossover, registered but only applied when `crossover_probability > 0` |
| `tools.HallOfFame(1)` | Tracks the single best individual seen across all generations |

The project does not use DEAP's built-in `eaSimple` or other algorithm helpers. The generation loop, mutation scheduling, checkpoint logic, and evaluation caching are all implemented in `candidate.py` directly, with DEAP providing only the operator primitives listed above.

## How the Project Wraps DEAP

The full-genome balancer defines a `CandidateWorkflow` abstract class in `tools/auto_balancer/workflows/candidate.py`. Concrete subclasses implement:

- `build_initial_population` - how the starting population is constructed (seeded from the existing content values plus random candidates within defined bounds)
- `mutate_individual` - how a candidate is perturbed (per-gene step-size mutation)
- `evaluate_candidate` - calls the C# CLI to run simulations and returns a measurement object
- `get_fitness` - reduces a measurement to a scalar fitness score
- `normalize_individual` - clamps gene values to their valid bounds after mutation

`run_candidate_workflow` wires these methods into DEAP's Toolbox and runs the generation loop, handling checkpointing and keyboard-interrupt recovery around DEAP's structures.

## Checkpointing and RNG

Between generations, `candidate.py` serialises the full DEAP population, the hall of fame, the measurement cache, and both the `random.Random` instance state and the global `random` module state into a JSON checkpoint file. This allows a run to be interrupted and resumed without re-evaluating already-seen candidates. The RNG states are pickled and base64-encoded into the JSON payload.
