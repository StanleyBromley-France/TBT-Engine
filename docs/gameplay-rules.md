# Gameplay Rules

This project simulates turn-based tactical combat on a hex grid. It is designed for automated evaluation rather than direct player interaction, but the engine still follows concrete game rules for movement, abilities, effects, turns, and victory.

## Core Loop

A match starts from a game state loaded from content. The game state defines:

- the map dimensions and terrain distribution
- the attacker and defender team ids
- which team acts first (always attacker)
- the units on each team
- each unit's starting hex position

During evaluation, AI controllers choose actions for both teams until a victory condition is reached.

## Teams And Roles

The engine supports arbitrary team ids, but the full-genome evaluation setup uses:

- attacker team: `1`
- defender team: `2`

Units have a primary role and may have a secondary role.

Primary roles are:

- `Tank`
- `Healer`
- `Damage`

Secondary roles include:

- `Buffer`
- `Debuffer`
- `Acrobat` - present in content, but Acrobat-specific movement scoring is unused in the no-move variant
- `null` for no secondary role

The roles do not directly create special rules by themselves. Instead, they describe the intended function of the unit and are used by the full-genome balancer to group units and measure behaviour.

## Hex Grid And Terrain

The game uses axial hex coordinates, written as `q` and `r`.

Terrain types are:

- `Plain`: walkable.
- `Mountain`: blocked.
- `Water`: blocked.

Movement and range are based on hex-grid distance. Generated scenarios choose map dimensions and terrain distributions from scenario configs.

In the no-move variant, the engine still supports movement, but the content/config disables movement as a balance variable: units have `movePoints` set to `0`, move-point genome deltas are fixed to `0`, and movement-based scoring targets are disabled.

## Unit Resources

Units have runtime resources:

- HP
- mana
- move points
- action points

HP determines whether a unit is alive. Mana is spent on abilities. Move points are spent when moving. Action points are spent by moving, using an ability, or skipping.

At the start of a unit's active opportunity, its available resources are based on its template and derived stats. Effects can change derived stats, which can affect maximum HP, maximum mana, move points, action points, damage, healing, and damage received.

## Actions

The engine currently supports three action types:

- move
- use ability
- skip active unit

The action generator produces legal actions for living units on the team whose turn it is. Units that have already committed during the current team turn cannot act again.

If a unit has started acting and still has action points, the engine keeps that unit as the currently committing unit. This prevents switching freely between units mid-commitment.

## Movement

A move action is legal when:

- the acting unit is alive
- the acting unit belongs to the team whose turn it is
- the unit has action points
- the target hex is unoccupied
- the target hex is reachable using the unit's current move points
- the path only uses walkable terrain

Moving changes the unit's position, spends the movement cost, and spends one action point.

This rule is unused in normal no-move simulations because units have no move points.

## Abilities

An ability action is legal when:

- the acting unit is alive
- the acting unit belongs to the team whose turn it is
- the unit has action points
- the unit has the selected ability in its `abilityIds`
- the unit has enough mana for the ability's `manaCost`
- the target matches the ability's `allowedTarget`
- the target is within the ability's `range`
- line of sight exists when `requiresLineOfSight` is true

Using an ability spends mana, applies the linked effect template, and spends one action point.

Target types are:

- `Self`: the unit targets itself.
- `Ally`: the unit targets a living allied unit other than itself.
- `Enemy`: the unit targets a living enemy unit.

If an ability has `radius` greater than `0`, the chosen target becomes the centre of an area. The effect is applied to all living units in that area that match the ability's target type.

## Effects

Abilities apply effect templates. An effect template contains one or more effect component templates.

Components can:

- deal instant damage
- heal instantly
- deal damage over time
- heal over time
- apply flat stat modifiers
- apply percentage stat modifiers

Effects have:

- `totalTicks`: duration
- `maxStacks`: stack limit
- `isHarmful`: whether the effect is harmful

Repeated applications of the same effect can stack up to `maxStacks`. Active effects contribute to derived stats and can resolve over time.

## Derived Stats

Derived stats are recalculated from a unit's base template plus active effect modifiers.

Derived stats include:

- maximum HP
- maximum mana
- maximum move points
- maximum action points
- damage dealt
- healing dealt
- healing received
- physical damage received
- magical damage received

Flat modifiers add or subtract fixed values. Percentage modifiers apply percentage-point changes.

For stat modifiers, the engine tracks dominant buff and debuff contributions. Buffs and debuffs do not simply stack without limit; the strongest positive and strongest negative contributions are selected for each stat/modifier type, with deterministic tie-breaking.

## Damage And Healing

Damage components specify a base damage value and damage type:

- `Physical`
- `Magical`

Incoming damage is affected by the target's relevant damage-received stat. A physical attack uses `PhysicalDamageReceived`, and a magical attack uses `MagicDamageReceived`. A value of `100` means normal damage, lower values reduce damage, and higher values increase damage.

Healing components restore HP and can be modified by healing-related derived stats.

Damage and healing components may also define critical hit/heal fields where supported:

- `critChance`
- `critMultiplier`

## Turn Flow

Each team turn allows that team's uncommitted living units to act. A unit commits when it reaches zero action points or chooses to skip.

The attacker turn counter tracks how many attacker turns have been taken. This counter is important because evaluation scenarios often use an attacker turn limit.

The exact order of legal actions is less important for automated evaluation because the AI controller chooses among generated legal actions.

## Victory Conditions

The engine uses a composite victory evaluator. The first non-ongoing result wins.

Current victory conditions are:

- last team standing
- defender victory when the attacker turn limit is exceeded

If only one team has living units, that team wins. If no teams have living units, the match is a draw.

For turn-limited evaluation, if `AttackerTurnsTaken` becomes greater than the configured maximum attacker turns, the defender wins.

## AI-Controlled Play

The evaluation runner uses MCTS player controllers. MCTS searches possible action sequences and scores resulting states using the configured agent profile.

The agent can use different profiles, such as:

- balanced
- offensive
- defensive

The details of the MCTS scoring profile are documented in [MCTS weights and promoted behaviour](mcts-weights.md).

## What The Simulation Is For

The gameplay simulation is intended to produce repeatable evidence for balancing. It is not a full user-facing game.

This means:

- matches are automated
- results are recorded as metrics
- randomness is controlled by seeds
- content is evaluated through repeated scenarios
- balance changes are judged through simulation outcomes and role-behaviour measurements

The simulation can reveal obvious balance problems and compare candidate content changes, but final balance decisions should still be interpreted alongside design judgement and human playtesting.
