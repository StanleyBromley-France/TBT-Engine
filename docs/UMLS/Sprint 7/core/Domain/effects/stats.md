# Unit Stats & Effect Interaction Specification

## 1. Purpose

This document defines the runtime stat model for units and how active effects interact with it.

It standardises:

- Stat categories  
- Storage ownership  
- Effect contribution rules  
- Combination & stacking rules  
- Recalculation triggers  
- Determinism guarantees  

It assumes:

- Effect stacking / refresh / replacement has already been resolved.  
- `GameState.ActiveEffectsByUnit[unitId]` contains the correct active `EffectInstance`s.  

---

## 2. Runtime Stat Categories

A unit has four distinct categories of runtime values.

---

### 2.1 BaseStats

#### Definition

- Defined in `UnitTemplate`.
- Immutable during gameplay.
- Never modified by effects or mutators.

#### Storage Location

- Stored only in `UnitTemplate.BaseStats`.
- Not duplicated in `UnitInstance`.

#### Purpose

- Provide baseline values for calculations.
- Serve as the source of truth for DerivedStats.

---

### 2.2 DerivedStats

#### Definition

A computed projection of:

- `UnitTemplate.BaseStats`
- Active effects on the unit (`GameState.ActiveEffectsByUnit[unitId]`)

DerivedStats are stored on the unit but are never directly mutated by gameplay.

#### Storage Location

- Stored in `UnitInstance.DerivedStats`.
- Overwritten whenever recomputed.

#### Properties

- Recomputed from the active-effect snapshot.
- Deterministic (same snapshot → same result).

#### Computation Summary

DerivedStats are produced by applying effect component contributions according to the rules in **Section 4**.

Percentage-based contributions are converted into flat deltas derived from the original `BaseStats` and do not multiply the final computed value.

#### Recalculation Triggers

Recompute DerivedStats when:

- An effect is added
- An effect is removed
- An effect expires
- An effect’s stack count changes
- Any stat-relevant component value changes (if component values can change at runtime)

---

### 2.3 Resources

#### Definition

Mutable runtime values representing current state.

#### Storage Location

- Stored in `UnitInstance.Resources`.

#### Examples

- Current HP
- Current Mana
- Action Points
- Movement Points

#### Rules

- Mutated directly via mutators.
- Not part of DerivedStats calculation.
- May be clamped by DerivedStats (e.g., `CurrentHP <= DerivedStats.MaxHP`).

---

### 2.4 Effect-Derived Properties

Some effects modify gameplay behaviour without directly modifying numeric stats.

#### Storage Location

- Not stored on the unit.
- Computed on demand from active effects (query-time).

#### Examples

- Healing reduction
- Damage multipliers
- Critical chance modifiers
- Boolean states (Stunned, Silenced, etc.)

#### Rules

- Derived exclusively from the current active-effect snapshot.
- Not cached in `UnitInstance`.

---

## 3. Effect Identity & Independence

### 3.1 Effect Template Identity

- Each `EffectTemplate` creates its own `EffectInstance`.
- Stacking / refresh / replacement is evaluated only between instances of the same `EffectTemplateId`.
- Different templates never merge components.

### 3.2 Cross-Template Independence

Effects from different templates:

- Remain independent
- Maintain separate duration and stacks
- Do not merge component instances

---

## 4. Combination & Stacking Rules (Single Source of Truth)

This section defines how multiple effect contributions combine.

### 4.1 Key Principle: No Cross-Instance Stacking

When multiple **different EffectInstances** contribute to the same “thing” (same stat, same property, same flag):

- They do **not** stack additively.
- Only a single dominant buff and a single dominant debuff apply per stat or property.
- Dominance is decided by context:
  - choose **MAX** when “higher is stronger”
  - choose **MIN** when “lower is stronger”

Buffs and debuffs are evaluated independently and both may apply.

This rule applies to:

- DerivedStats modifiers
- Effect-derived properties (healing reduction, multipliers, etc.)
- Any other computed gameplay property derived from effects

### 4.2 What Counts as “the Same Thing”

Two contributions are considered competing for the same thing if they target the same:

- `UnitStatType` (for DerivedStats), or
- property kind (e.g. `HealingReduction`, `DamageTakenMultiplier`), or
- boolean state (e.g. `Stunned`)

In other words: same *target* and same *meaning* → compete → dominance rules decide the winner.

### 4.3 Stacking Within the Same EffectInstance

If the contribution comes from the same effect instance/component instance, and the effect has stacks:

- stacks change the strength of that single contribution
- stack math depends on the component’s value type

#### Flat integer modifiers (ADD per stack)

For components expressed as an `int` amount:

effectiveDelta = amountPerStack * stacks


Example: `+3 Attack` with 4 stacks → `+12 Attack`

#### Percentage modifiers (Percent-of-base, MULTIPLY per stack internally)

For components expressed as a percentage per stack:

effectivePercentAdd = ((1 + percentPerStack) ^ stacks) - 1


This value represents a percentage of the original `BaseStat`.

At application time:

percentDelta = BaseStat * effectivePercentAdd


Percentage contributions are additive relative to `BaseStat` and do not compound on other modifiers.

### 4.4 Deterministic Tie-Breaking

If two competing contributions are equal under MAX/MIN:

- break ties deterministically using a stable rule (e.g. lowest `EffectInstanceId` wins)
- the rule must not depend on iteration order

---

## 5. DerivedStats Application Order

To keep results predictable, DerivedStats are computed in a fixed order:

1. Start from `UnitTemplate.BaseStats`
2. Apply dominant flat buff
3. Apply dominant flat debuff
4. Convert dominant percent buff into flat delta from `BaseStat` and apply
5. Convert dominant percent debuff into flat delta from `BaseStat` and apply
6. Apply clamps (if defined)
7. Write to `UnitInstance.DerivedStats`

This order is fixed and does not depend on effect order.

---

## 6. Determinism

Given:

- Fixed `BaseStats`
- Fixed set of active `EffectInstance`s (including stacks)

The following must always produce identical results:

- DerivedStats computation
- Effect-derived property queries

Determinism is guaranteed by:

- recomputing from the active-effect snapshot
- MAX/MIN dominance rules (no cross-instance stacking)
- deterministic stack maths within an instance
- deterministic tie-breaking

---

## 7. Invariants

- `UnitTemplate.BaseStats` never changes.
- DerivedStats are never directly mutated by gameplay; they are recomputed.
- Resources are mutated only via mutators.
- Effect-derived properties are computed on demand and are not cached.
- EffectInstances never merge across templates.
- Different EffectInstances never stack contributions for the same thing; dominance rules decide the winner.