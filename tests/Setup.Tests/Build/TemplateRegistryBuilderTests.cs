namespace Setup.Tests.Build;

using Setup.Build.TemplateRegistry;
using Setup.Build.TemplateRegistry.Builders.EffectComponents;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;
using Setup.Build.TemplateRegistry.Results;
using Setup.Config;
using Setup.Validation.Primitives;

public sealed class TemplateRegistryBuilderTests
{
    [Fact]
    public void Build_Strict_Returns_Null_Registry_When_Errors_Exist()
    {
        var input = CreateValidInput();
        input.Abilities[0].EffectTemplateId = "missing-effect";

        var result = Build(input, ContentValidationMode.Strict);

        Assert.Null(result.TemplateRegistry);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Build_Lenient_Aggregates_Issues_And_Builds_What_Is_Safe()
    {
        var input = CreateValidInput();
        input.Abilities.Add(new AbilityConfig
        {
            Id = "ability-broken",
            Name = "Broken",
            Category = "MeleeAttack",
            ManaCost = 1,
            Targeting = new TargetingRulesConfig
            {
                Range = 1,
                RequiresLineOfSight = false,
                AllowedTarget = "Enemy",
                Radius = 0
            },
            EffectTemplateId = "missing-effect"
        });
        input.UnitTemplates[0].AbilityIds.Add("ability-broken");

        var result = Build(input, ContentValidationMode.Lenient);

        Assert.NotNull(result.TemplateRegistry);
        Assert.True(result.Issues.Count > 0);
        Assert.Contains(result.Issues, i => i.Path == "Abilities[1].EffectTemplateId");
        Assert.Single(result.TemplateRegistry!.Abilities.GetAll());
    }

    [Fact]
    public void Build_Succeeds_For_Valid_Content()
    {
        var input = CreateValidInput();

        var result = Build(input, ContentValidationMode.Strict);

        Assert.NotNull(result.TemplateRegistry);
        Assert.False(result.HasErrors);
        Assert.Single(result.TemplateRegistry!.Units.GetAll());
        Assert.Single(result.TemplateRegistry.Abilities.GetAll());
        Assert.Single(result.TemplateRegistry.Effects.GetAll());
        Assert.Single(result.TemplateRegistry.EffectComponents.GetAll());
    }

    private static TemplateRegistryBuildResult Build(TestInput input, ContentValidationMode mode)
    {
        var strategies = new Dictionary<string, IEffectComponentBuilder>(StringComparer.OrdinalIgnoreCase)
        {
            ["instantdamage"] = new InstantDamageComponentBuilder(),
            ["damageovertime"] = new DamageOverTimeComponentBuilder(),
            ["instantheal"] = new InstantHealComponentBuilder(),
            ["healovertime"] = new HealOverTimeComponentBuilder(),
            ["flatattributemodifier"] = new FlatAttributeModifierComponentBuilder(),
            ["percentattributemodifier"] = new PercentAttributeModifierComponentBuilder()
        };

        var resolver = new EffectComponentBuilderResolver(strategies);
        var builder = new TemplateRegistryBuilder(resolver);
        return builder.Build(
            input.UnitTemplates,
            input.Abilities,
            input.EffectTemplates,
            input.EffectComponentTemplates,
            mode);
    }

    private static TestInput CreateValidInput()
    {
        return new TestInput
        {
            UnitTemplates =
            [
                new UnitTemplateConfig
                {
                    Id = "unit-1",
                    Name = "Soldier",
                    MaxHP = 100,
                    MaxManaPoints = 10,
                    MovePoints = 5,
                    PhysicalDamageReceived = 100,
                    MagicDamageReceived = 100,
                    AbilityIds = ["ability-1"]
                }
            ],
            Abilities =
            [
                new AbilityConfig
                {
                    Id = "ability-1",
                    Name = "Strike",
                    Category = "MeleeAttack",
                    ManaCost = 2,
                    Targeting = new TargetingRulesConfig
                    {
                        Range = 1,
                        RequiresLineOfSight = false,
                        AllowedTarget = "Enemy",
                        Radius = 0
                    },
                    EffectTemplateId = "effect-1"
                }
            ],
            EffectTemplates =
            [
                new EffectTemplateConfig
                {
                    Id = "effect-1",
                    Name = "StrikeEffect",
                    IsHarmful = true,
                    TotalTicks = 1,
                    MaxStacks = 1,
                    ComponentTemplateIds = ["component-1"]
                }
            ],
            EffectComponentTemplates =
            [
                new EffectComponentTemplateConfig
                {
                    Id = "component-1",
                    Type = "InstantDamage",
                    Damage = 10,
                    DamageType = "Physical",
                    CritChance = 0,
                    CritMultiplier = 1f
                }
            ]
        };
    }

    private sealed class TestInput
    {
        public List<UnitTemplateConfig> UnitTemplates { get; set; } = new();
        public List<AbilityConfig> Abilities { get; set; } = new();
        public List<EffectTemplateConfig> EffectTemplates { get; set; } = new();
        public List<EffectComponentTemplateConfig> EffectComponentTemplates { get; set; } = new();
    }
}
