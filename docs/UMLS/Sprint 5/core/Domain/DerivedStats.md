# Derived Stats Rules

This document defines how a unit’s **DerivedStats** are calculated from its **BaseStats** and the set of **active effects stored in GameState**.

It intentionally does **not** redefine effect semantics. It assumes:

* Effect stacking / refresh / replacement has already been resolved.
* `GameState.ActiveEffectsByUnit[unitId]` contains the correct set of active `EffectInstance`s.

---

## 1. Stat Categories

* **BaseStats**

  * Immutable baseline values from `UnitTemplate`.
  * Never modified at runtime.

* **DerivedStats**

  * Computed values used for gameplay calculations.
  * Not directly mutated by `GameMutationContext`.
  * Recomputed when the active-effect set changes.

* **Resources**

  * Mutable runtime values (HP, Mana, AP, MP, etc.).
  * Not part of derived stat calculation.

---

## 2. Source of Truth

DerivedStats are a projection of:

* `UnitTemplate.BaseStats`
* all active `EffectInstance`s on the unit in `GameState`

There is no incremental “apply + undo” of stat deltas.
DerivedStats are always recomputed from the current active-effect snapshot.

---

## 3. When Derived Stats Are Recomputed

A dedicated call must run:

`UpdateDerivedStats(GameState state, UnitInstance unit)`

It must be invoked whenever any of the following change for that unit:

* an EffectInstance is added
* an EffectInstance is removed or expires
* an EffectInstance’s stack count changes
* an EffectInstance component value changes (if that exists)

---

## 4. Stat Contributions from Effects

Only components that explicitly modify stats contribute to DerivedStats.

Given the current effect model:

* `StatModifierComponentTemplate` contains:

  * `UnitStatType Stat`
  * `int ModifierAmount`

This implies:

* stat modifiers are **flat additive**
* magnitude may depend on the EffectInstance’s stack count

---

## 5. Contribution Formula

For each active `EffectInstance e` on the unit
for each stat-modifier component `c` in `e.Components`:

```
contribution = c.ModifierAmount * e.CurrentStacks
```

If an effect does not use stacks, `e.CurrentStacks` is treated as `1`.

---

## 6. Aggregation Across Effects

Because stat modifiers are flat additive:

For each `UnitStatType stat`:

```
totalContribution(stat) =
    sum of (c.ModifierAmount * e.CurrentStacks)
    across all active effects e
    and all stat-modifier components c targeting stat
```

No non-additive aggregation (MAX/MIN/MULTIPLY) is used for stats unless a new
stat-modifier component type is explicitly introduced later.

---

## 7. Derived Stats Algorithm

Given:

* `base = unit.Template.BaseStats`
* `effects = state.ActiveEffectsByUnit[unit.Id]`

Compute:

1. Start with a working copy:

```
derived = base
```

2. For each active effect `e in effects`:

   * For each component `comp in e.Components`:

     * If `comp` is a stat-modifier component:

       ```
       derived[comp.Stat] += comp.ModifierAmount * e.CurrentStacks
       ```

3. Apply any stat-specific clamps (if defined).

4. Write the result into:

```
unit.DerivedStats
```

---


## 8. Determinism

Given:

* a fixed `BaseStats`
* a fixed set of active `EffectInstance`s in `GameState`

`UpdateDerivedStats` must always produce the same result,
independent of effect application order.

This is guaranteed by:

* recomputing from the active-effect snapshot
* summing flat contributions only.

---

## 9. Invariants

* `UnitTemplate.BaseStats` never changes.
* `DerivedStats` are never directly mutated by gameplay events.
* `DerivedStats` always reflect:

  * the current `BaseStats`
  * the current set of active effects in `GameState`.
