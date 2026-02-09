using Core.Domain.Units.Instances;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Templates;

namespace Core.Tests.Domain.Units;

public class UnitTests
{
    private static UnitBaseStats CreateDefaultStats()
        => new UnitBaseStats(
            maxHp: 100,
            movePoints: 3,
            maxMana: 20,
            maxArmourPoints: 1);

    private static UnitTemplate CreateDefaultTemplate()
        => new UnitTemplate(
            id: new UnitTemplateId("default"),
            name: "Default Unit",
            baseStats: CreateDefaultStats(),
            abilityIds: new[] { "BasicAttack" });


    [Fact]
    public void IsAlive_Returns_True_When_CurrentHP_Greater_Than_Zero()
    {
        var template = CreateDefaultTemplate();
        var unit = new UnitInstance(
            id: new UnitInstanceId(123),
            team: Team.Defender,
            template: template,
            startPosition: new Position(0, 0)
        );

        unit.CurrentHP = 1;

        Assert.True(unit.IsAlive);
    }

    [Fact]
    public void IsAlive_Returns_False_When_CurrentHP_Is_Zero()
    {
        var template = CreateDefaultTemplate();
        var unit = new UnitInstance(
            id: new UnitInstanceId(123),
            team: Team.Defender,
            template: template,
            startPosition: new Position(0, 0)
        );

        unit.CurrentHP = 0;

        Assert.False(unit.IsAlive);
    }

    [Fact]
    public void IsAlive_Returns_False_When_CurrentHP_Is_Negative()
    {
        var template = CreateDefaultTemplate();
        var unit = new UnitInstance(
            id: new UnitInstanceId(123),
            team: Team.Defender,
            template: template,
            startPosition: new Position(0, 0)
        );

        unit.CurrentHP = -1;

        Assert.False(unit.IsAlive);
    }
}
