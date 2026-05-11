# Content Schema

The engine loads gameplay content from JSON files in a content directory. The default content directory is `content/`, and alternate content packs, such as `content-no-move/`, follow the same schema.

The main files are:

- `unitTemplates.json`
- `abilities.json`
- `effectTemplates.json`
- `effectComponentTemplates.json`
- `gameStates.json`

These files form a reference chain:

```text
gameStates.json
  -> unitTemplates.json
      -> abilities.json
          -> effectTemplates.json
              -> effectComponentTemplates.json
```

## Unit Templates

`unitTemplates.json` defines the available unit types.

Each unit template has:

- `id`: unique unit template id.
- `name`: display/debug name.
- `primaryRole`: main role, currently `Tank`, `Healer`, or `Damage`.
- `secondaryRole`: optional secondary role, such as `Buffer`, `Debuffer`, or `Acrobat`; can be `null`. Acrobat content may exist in the no-move content pack, but Acrobat-specific movement scoring and separate Acrobat genome profiles are unused in the no-move variant.
- `maxHP`: starting and maximum HP before derived stat modifiers.
- `maxManaPoints`: starting and maximum mana before derived stat modifiers.
- `movePoints`: movement budget before derived stat modifiers. In the no-move content pack, this is set to `0` and movement tuning is unused.
- `physicalDamageReceived`: percentage of incoming physical damage received, where `100` means normal damage and lower values mean more resistance.
- `magicDamageReceived`: percentage of incoming magical damage received, where `100` means normal damage and lower values mean more resistance.
- `abilityIds`: ids of abilities this unit can use.

Example:

```json
{
  "id": "unit-tank-one",
  "name": "Tank One",
  "primaryRole": "Tank",
  "secondaryRole": null,
  "maxHP": 1040,
  "maxManaPoints": 180,
  "movePoints": 2,
  "physicalDamageReceived": 82,
  "magicDamageReceived": 106,
  "abilityIds": [
    "ability-tank-one-basic-strike"
  ]
}
```

## Abilities

`abilities.json` defines actions that units can perform.

Each ability has:

- `id`: unique ability id.
- `name`: display/debug name.
- `category`: ability category, currently `Melee`, `Ranged`, or `Self`.
- `manaCost`: mana required to use the ability.
- `targeting`: target rules for the ability.
- `effectTemplateId`: effect template applied when the ability resolves.

The `targeting` object has:

- `range`: maximum hex distance to the chosen target. In the no-move variant, authored ranges are used, but ranged range tuning is unused.
- `requiresLineOfSight`: whether terrain line of sight is checked.
- `allowedTarget`: target type, currently `Self`, `Ally`, or `Enemy`.
- `radius`: area-of-effect radius around the chosen target. `0` means single-target. The full-genome balancer does not change this value.

Example:

```json
{
  "id": "ability-tank-two-area-strike",
  "name": "Tank Two Area Strike",
  "category": "Melee",
  "manaCost": 40,
  "targeting": {
    "range": 1,
    "requiresLineOfSight": false,
    "allowedTarget": "Enemy",
    "radius": 1
  },
  "effectTemplateId": "effect-tank-two-area-strike"
}
```

When `radius` is greater than `0`, the chosen target acts as the centre of the area. The effect is applied to valid units within that radius that also match the ability's `allowedTarget`.

The full-genome balancer may tune ability `manaCost` and, in the standard movement-enabled variant, ranged ability `range`. It does not change ability category, allowed target type, line-of-sight requirements, area radius, or effect-template references.

## Effect Templates

`effectTemplates.json` defines effects that can be applied to units.

Each effect template has:

- `id`: unique effect template id.
- `name`: display/debug name.
- `isHarmful`: whether the effect is considered harmful.
- `totalTicks`: how long the effect lasts. A value of `1` is effectively immediate.
- `maxStacks`: maximum stack count for repeated applications.
- `componentTemplateIds`: ids of effect components that make up the effect.

Example:

```json
{
  "id": "effect-tank-one-basic-strike",
  "name": "Tank One Basic Strike Effect",
  "isHarmful": true,
  "totalTicks": 1,
  "maxStacks": 1,
  "componentTemplateIds": [
    "component-tank-one-basic-strike-damage"
  ]
}
```

An effect can contain one or more components. Components do the actual damage, healing, or stat modification.

## Effect Component Templates

`effectComponentTemplates.json` defines the atomic parts of effects.

Supported component types include:

- `InstantDamage`
- `InstantHeal`
- `DamageOverTime`
- `HealOverTime`
- `FlatAttributeModifier`
- `PercentAttributeModifier`

Damage components use:

- `damage`: base damage amount.
- `damageType`: `Physical` or `Magical`.
- `critChance`: optional critical hit chance.
- `critMultiplier`: optional critical hit multiplier.

Healing components use:

- `heal`: base healing amount.
- `critChance`: optional critical heal chance.
- `critMultiplier`: optional critical heal multiplier.

Flat stat modifiers use:

- `stat`: stat to modify.
- `amount`: additive change.

Percent stat modifiers use:

- `stat`: stat to modify.
- `percent`: percentage-point change.

Supported stat names are:

- `MaxHP`
- `MaxManaPoints`
- `MovePoints` - fixed/unused for tuning in the no-move variant
- `ActionPoints`
- `DamageDealt`
- `HealingDealt`
- `HealingReceived`
- `PhysicalDamageReceived`
- `MagicDamageReceived`

Example damage component:

```json
{
  "id": "component-tank-one-basic-strike-damage",
  "type": "InstantDamage",
  "damage": 80,
  "damageType": "Physical"
}
```

Example stat modifier:

```json
{
  "id": "component-tank-buffer-four-movement-buff-movepoints",
  "type": "FlatAttributeModifier",
  "stat": "MovePoints",
  "amount": 1
}
```

## Game States

`gameStates.json` defines playable/evaluable scenarios.

Each game state has:

- `id`: unique scenario id.
- `mapGen`: map generation settings.
- `attackerTeamId`: team id treated as the attacker.
- `defenderTeamId`: team id treated as the defender.
- `teamToAct`: team that acts first.
- `attackerTurnsTaken`: attacker turn counter at scenario start.
- `units`: unit spawns.

`mapGen` has:

- `width`: map width in offset-grid columns.
- `height`: map height in offset-grid rows.
- `tileDistribution`: terrain probability weights.

Supported terrain types are:

- `Plain`: walkable.
- `Mountain`: not walkable.
- `Water`: not walkable.

Each unit spawn has:

- `id`: unit template id.
- `teamId`: team to spawn the unit on.
- `q`: axial hex coordinate q.
- `r`: axial hex coordinate r.

Example:

```json
{
  "id": "refined-scenario-1",
  "mapGen": {
    "width": 6,
    "height": 4,
    "tileDistribution": {
      "Plain": 0.8,
      "Mountain": 0.12,
      "Water": 0.08
    }
  },
  "attackerTeamId": 1,
  "defenderTeamId": 2,
  "teamToAct": 1,
  "attackerTurnsTaken": 0,
  "units": [
    {
      "id": "unit-tank-one",
      "teamId": 1,
      "q": 0,
      "r": 1
    }
  ]
}
```

## Validation Expectations

Content ids should be unique within their file. References should point to existing ids:

- every unit `abilityIds` entry should exist in `abilities.json`
- every ability `effectTemplateId` should exist in `effectTemplates.json`
- every effect `componentTemplateIds` entry should exist in `effectComponentTemplates.json`
- every game state unit `id` should exist in `unitTemplates.json`

Numeric combat values should remain positive where they represent capacities, costs, damage, healing, or movement. Damage-received percentages are interpreted as percentages of incoming damage, so values below `100` reduce received damage and values above `100` increase it. The no-move content pack is the exception for movement capacity: `movePoints` is intentionally `0`.

## Content Packs

A content pack is any directory containing the five schema files above. The CLI and balancing tools can load alternate packs with options such as `--content` or `--input-package`.

The balancing tools usually work on generated copies of content rather than editing `content/` directly. Output balance packages include a full `content/` directory so they can be reused as input for later runs.

## Balancer-Tuned Fields

The full-genome balancer only changes a focused subset of content fields. This section is a schema-level summary; the balancing workflow and genome are explained in [Full Genome Balancer](full-genome-balancer.md).

In `unitTemplates.json`, it can tune:

- `maxHP`
- `maxManaPoints`
- `movePoints` in the standard variant; fixed/unused in the no-move variant
- `physicalDamageReceived`
- `magicDamageReceived`

In `abilities.json`, it can tune:

- `manaCost`
- `targeting.range` in the standard variant; fixed/unused in the no-move variant

In `effectComponentTemplates.json`, it can tune numeric effect values such as:

- `damage`
- `heal`
- `amount`
- `percent`

It does not change structural fields such as:

- ids or names
- unit roles
- unit ability lists
- ability categories
- ability `targeting.allowedTarget`
- ability `targeting.requiresLineOfSight`
- ability `targeting.radius`
- ability `effectTemplateId`
- effect `isHarmful`
- effect `totalTicks`
- effect `maxStacks`
- effect `componentTemplateIds`
- effect component `type`
- effect component `damageType`
- scenario/map layout in `gameStates.json`
