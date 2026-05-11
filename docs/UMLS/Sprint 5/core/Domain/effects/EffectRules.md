# Effect & Component Interaction Rules

## 1. Effect-Level Ownership
- Each **EffectTemplate** creates its own **EffectInstance**.
- Stacking, refresh, and replacement rules are evaluated **only between instances of the same EffectTemplateId**.
- Different EffectTemplateIds never merge or stack their components together.

---

## 2. Component Independence Across Templates
- If two effects come from **different EffectTemplateIds**:
  - All their components are applied independently.
  - No component-level stacking or merging occurs between them.
  - Each effect instance maintains its own duration, ticks, and lifecycle.

Example:
- Effect A applies *Remove Healing 10%* for 2 turns.
- Effect B applies *Remove Healing 50%* for 3 turns.
- Both effects remain active independently.

---

## 3. Component Stacking Scope
- Component stacking is considered **only if** both components come from the **same EffectTemplateId**.
- Within the same template:
  - Stackable components combine according to that template’s stacking rules.
  - Non-stackable components follow that template’s replacement or refresh rules.

---

## 4. Non-Additive Component Resolution (e.g., Remove Healing)
Some components are not additive and resolve via aggregation rules.

For *Remove Healing*:
- Multiple active effects may each contribute a healing reduction value.
- The unit’s effective healing reduction is:

effectiveReduction = max(all active contributions)


Example:
- Effect A: 10% healing reduction
- Effect B: 50% healing reduction  
Result: **50% healing reduction applies**

---

## 5. Duration and Lifecycle
- Each EffectInstance tracks its own:
  - RemainingTicks
  - CurrentStacks (if applicable)
- Expiration or removal of one effect instance:
  - Removes only that instance’s contribution.
  - Does not affect other active effects, even if they share component types.

---

## 6. Aggregation Responsibility
- Components define how their values aggregate when multiple instances are active.
  - Examples:
    - Remove Healing → `MAX`
    - Flat Damage → `ADD`
    - Damage Multiplier → `MULTIPLY`
- Aggregation may be resolved:
  - At query time (e.g., when a heal is attempted), or
  - On apply/remove (recompute cached effective value)

---

## 7. Design Consequences
- EffectTemplates remain the unit of identity and stacking.
- Components never merge across templates.
- Cross-template interactions are resolved purely through aggregation rules.
- No shared component instances are required.

This model prioritizes:
- Determinism
- Simplicity
- Predictable stacking behavior
