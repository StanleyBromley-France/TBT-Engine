# MCTS Weights And Promoted Behaviour

This document explains how the MCTS agent scores game states and what each weight is meant to encourage.

The relevant implementation lives in:

- `src/Agents.Mcts/Evaluation/MaterialStateEvaluator.cs`
- `src/Agents.Mcts/Config/MctsAgentProfile.cs`
- `src/Agents.Mcts/Search/MctsSearch.cs`
- `src/Agents.Mcts/Config/MctsSearchConfig.cs`

## Evaluation formula

For non-terminal states, the evaluator scores:

```text
score =
  allyAliveCount * AllyUnitWeight
  - enemyAliveCount * EnemyUnitWeight
  + allyHp * AllyHpWeight
  - enemyHp * EnemyHpWeight
  + allyResourceCapacityScore * AllyResourceCapacityWeight
  - enemyResourceCapacityScore * EnemyResourceCapacityWeight
  + allyDerivedCombatScore * AllyDerivedCombatWeight
  - enemyDerivedCombatScore * EnemyDerivedCombatWeight
  + allyMana * AllyManaWeight
  - enemyMana * EnemyManaWeight
  + remainingAttackerTurns * RemainingAttackerTurnsWeight
  + elapsedAttackerTurns * ElapsedAttackerTurnsWeight
```

For terminal states, `WinScore` dominates the heuristic terms so that wins and losses outweigh small positional differences.

## What each weight means

### Unit count

`AllyUnitWeight`

- Rewards keeping your own units alive.
- Higher values promote safer play, preserving board presence, and avoiding sacrificial trades.

`EnemyUnitWeight`

- Rewards removing enemy units.
- Higher values promote focus fire, kill confirms, and lines that convert pressure into permanent material advantage.

### Current HP

`AllyHpWeight`

- Rewards preserving current allied HP.
- Higher values promote safer trades, healing, retreating, and avoiding unnecessary chip damage.

`EnemyHpWeight`

- Rewards lowering current enemy HP.
- Higher values promote pressure, poke, burst setup, and lines that soften targets even before a kill is available.

### Resource capacity from derived stats

This is not current HP or current mana. It is the unit's maximum or capacity after buffs and debuffs.

The evaluator compares current derived values against the unit's base template values.

`AllyResourceCapacityWeight`

- Rewards allied increases to max HP, max mana, max move points, and max action points.
- Higher values promote buffing your own long-term efficiency and preserving those buffs.

`EnemyResourceCapacityWeight`

- Rewards states where enemy max HP, max mana, max move points, and max action points are reduced relative to base.
- Higher values promote debuffing enemies and benefiting from states where enemy long-term capacity is weakened.

### Derived combat modifiers

This captures combat effectiveness, again relative to the unit's base template values.

Included terms are:

- `DamageDealt`
- `HealingDealt`
- `HealingReceived`
- `PhysicalDamageReceived`
- `MagicDamageReceived`

For damage-received terms, lower values are better, so the evaluator inverts them.

`AllyDerivedCombatWeight`

- Rewards allied combat buffs and allied damage-mitigation improvements.
- Higher values promote maintaining strong buffs, defensive utility, and states where your units hit harder or survive better.

`EnemyDerivedCombatWeight`

- Penalizes enemy combat buffs and enemy mitigation improvements.
- Higher values promote lines where dangerous enemy buffs are removed, delayed, stranded on the wrong target, or made irrelevant by tempo.

Important nuance:

- This term is still useful even when the agent cannot directly stop a buff action.
- It is most useful when the agent can influence whether that buff matters.
- If enemy buffs are usually inevitable and do not change the best response, this term may be overweighted noise.

### Mana

`AllyManaWeight`

- Rewards preserving allied mana.
- Higher values promote resource conservation, future optionality, and avoiding wasteful spell use.

`EnemyManaWeight`

- Rewards draining enemy mana.
- Higher values promote attrition plans and lines that reduce enemy future options.

### Turn-pressure terms

These are specific to scenarios where attacker turn count matters.

`RemainingAttackerTurnsWeight`

- Rewards having more attacker turns left.
- Higher values promote faster wins and more urgency from the attacker's side.
- This is why the offensive profile prefers earlier progress.

`ElapsedAttackerTurnsWeight`

- Rewards more attacker turns already being spent.
- Higher values promote stalling, delaying, and surviving longer.
- This is why the defensive profile prefers slower games.

### Terminal value

`WinScore`

- Large terminal bonus for a win and large penalty for a loss.
- This ensures the search does not prefer "pretty" losing states over actual wins.

## Profile presets

The project currently defines three preset profiles.

| Weight | Balanced | Offensive | Defensive |
| --- | ---: | ---: | ---: |
| AllyUnitWeight | 100 | 95 | 140 |
| EnemyUnitWeight | 100 | 140 | 95 |
| AllyHpWeight | 1.0 | 0.8 | 1.35 |
| EnemyHpWeight | 1.0 | 1.35 | 0.8 |
| AllyResourceCapacityWeight | 20 | 16 | 24 |
| EnemyResourceCapacityWeight | 20 | 24 | 16 |
| AllyDerivedCombatWeight | 25 | 22 | 32 |
| EnemyDerivedCombatWeight | 25 | 32 | 22 |
| AllyManaWeight | 1.0 | 0.75 | 1.1 |
| EnemyManaWeight | 1.0 | 1.1 | 0.75 |
| RemainingAttackerTurnsWeight | 0 | 35 | 0 |
| ElapsedAttackerTurnsWeight | 0 | 0 | 35 |
| WinScore | 1,000,000 | 1,000,000 | 1,000,000 |

### Balanced

Promotes:

- even valuation of damage dealt vs damage avoided
- even valuation of ally preservation vs enemy removal
- moderate respect for buffs, debuffs, and mana
- no special bias toward rushing or stalling

Use when:

- you want stable general-purpose play
- you are still calibrating the heuristic
- you do not want tempo bias from attacker-turn pressure

### Offensive

Promotes:

- killing enemies over preserving your own units
- dealing damage over protecting HP
- valuing enemy debuffs more than allied self-buffs
- spending time budget aggressively to finish faster

Typical behaviour:

- stronger focus fire
- more willingness to trade HP for initiative
- more interest in burst, pressure, and racing

### Defensive

Promotes:

- preserving allied units and HP over pure enemy damage
- valuing self-buffs and survivability more than enemy attrition
- lasting longer and burning attacker turns

Typical behaviour:

- more conservative positioning
- more respect for safety and sustain
- more willingness to prolong the game if that improves outcome odds

## Opponent profile in rollouts

There are two profiles in `MctsSearchConfig`:

- `Profile`
- `OpponentProfile`

During heuristic rollouts:

- when the acting unit is on the root side, rollout action choice uses `Profile`
- when the acting unit is on the opposing side, rollout action choice uses `OpponentProfile`

This means rollouts model both sides as having different priorities.

Example:

- attacker can be modeled as `Offensive`
- defender can be modeled as `Defensive`

That does not mean the final state score uses both profiles equally. The final rollout evaluation is still scored from the root side's perspective.

## Tie-break rules

When actions are equal on score, the search prefers:

1. `UseAbilityAction`
2. `MoveAction`
3. `SkipActiveUnitAction`
4. `ChangeActiveUnitAction`

This promotes forward progress and reduces low-value oscillation when heuristic values tie.