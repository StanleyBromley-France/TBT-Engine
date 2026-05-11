# setup_layer

```mermaid
classDiagram
    direction LR

    %% ============================================================
    %% JSON / DTO CONFIG TYPES (EDITED BY GA / TOOLS)
    %% ============================================================

    %% --- Abilities / Effects ---

    class AbilityConfig {
        +string Id
        +string Name
        +string Category        %% maps to AbilityCategory
        +AbilityCostConfig Cost
        +TargetingRulesConfig Targeting
        +List~EffectTemplateConfig~ Effects
    }

    class AbilityCostConfig {
        +int Mana
    }

    class TargetingRulesConfig {
        +int Range
        +bool RequiresLineOfSight
        +List~string~ AllowedTargets    %% maps to TargetType enum names
        +AreaPatternConfig AreaPattern
        +bool IncludeSelf
    }

    class AreaPatternConfig {
        +string Shape   %% maps to AreaShape
        +int Radius
        +int Length
        +int Width
    }

    class EffectTemplateConfig {
        +string Id
        +string Name
        +bool IsHarmful
        +int TotalTicks
        +int MaxStacks
        +List~EffectComponentConfig~ Components
    }

    class EffectComponentConfig {
        +string Type             %% e.g. "Damage", "DamageOverTime", "Heal"...
        +int? Damage
        +int? DamagePerTick
        +string DamageType       %% maps to DamageType
        +int? Heal
        +int? HealPerTick
        +string Stat             %% maps to UnitStatType
        +int? ModifierAmount
    }

    %% --- Units ---

    class UnitConfig {
        +string Id               %% maps to UnitTemplateId
        +string Name
        +int MaxHP
        +int MovePoints
        +int MaxActionPoints
        +int MaxMana
        +int Armor
        +List~string~ AbilityIds %% maps to AbilityId
    }

    %% --- Map generation ---

    class MapGenConfig {
        +int Width
        +int Height
        +Dictionary~string, float~ TileDistribution %% tileType -> percentage
    }

    %% --- Scenario (map + units + seed) ---

    class ScenarioUnitSpawnConfig {
        +string TemplateId       %% UnitTemplateId as string
        +Team Team
        +int X
        +int Y
    }

    class ScenarioConfig {
        +string Id
        +MapGenConfig MapGen
        +int Seed
        +Team FirstTeamToAct
        +List~ScenarioUnitSpawnConfig~ Units
    }

    %% config relationships
    AbilityConfig *-- AbilityCostConfig
    AbilityConfig *-- TargetingRulesConfig
    AbilityConfig o-- EffectTemplateConfig

    TargetingRulesConfig *-- AreaPatternConfig
    EffectTemplateConfig o-- EffectComponentConfig

    UnitConfig --> Team

    ScenarioConfig *-- MapGenConfig
    ScenarioConfig *-- ScenarioUnitSpawnConfig
    ScenarioUnitSpawnConfig --> Team

    %% ============================================================
    %% FACTORIES / MAPPERS / GENERATORS (CONFIG -> DOMAIN)
    %% ============================================================

    class AbilityFactory {
        +Ability FromConfig(AbilityConfig cfg)
        +EffectTemplate FromTemplateConfig(EffectTemplateConfig cfg)
        +EffectComponentTemplate FromComponentConfig(EffectComponentConfig cfg)
    }

    class UnitFactory {
        +UnitTemplate FromConfig(UnitConfig cfg, IAbilityRepository abilities)
        +UnitInstance CreateInstance(UnitTemplate template, Team team, HexCoord position, int instanceIndex)
    }

    class MapGenerator {
        +Map Generate(MapGenConfig cfg, RngState rngState)
    }

    class GameSetup {
        +GameState BuildInitialGameState(ScenarioConfig scenario)
    }

    %% factories / generators use config and repos
    AbilityFactory ..> AbilityConfig
    AbilityFactory ..> EffectTemplateConfig
    AbilityFactory ..> EffectComponentConfig

    UnitFactory ..> UnitConfig
    UnitFactory ..> IAbilityRepository
    UnitFactory ..> IUnitConfigRepository

    MapGenerator ..> MapGenConfig

    GameSetup ..> ScenarioConfig
    GameSetup ..> MapGenerator
    GameSetup ..> UnitFactory
    GameSetup ..> IAbilityRepository
    GameSetup ..> IUnitConfigRepository

    %% ============================================================
    %% REPOSITORIES (DOMAIN / CONFIG PROVIDERS)
    %% ============================================================

    class IAbilityRepository {
        <<interface>>
        +Ability GetById(AbilityId id)
        +IReadOnlyDictionary~AbilityId, Ability~ GetAll()
    }

    class IUnitConfigRepository {
        <<interface>>
        +UnitConfig GetById(UnitTemplateId id)
        +IReadOnlyDictionary~UnitTemplateId, UnitConfig~ GetAll()
    }

```
