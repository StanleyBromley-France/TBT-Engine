namespace Setup.Tests.Loading;

using Setup.Loading;
using Setup.Validation.Primitives;

public sealed class JsonContentLoaderRequiredFieldsPopulatedTests
{
    [Fact]
    public void LoadFromFiles_With_Example_Files_Loads_All_Required_Values()
    {
        var pack = JsonContentLoader.LoadFromFiles(LoaderFixturePaths.Example);

        Assert.False(pack.HasErrors);
        Assert.DoesNotContain(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode);
        Assert.DoesNotContain(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode);
        Assert.DoesNotContain(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullItemCode);

        Assert.Single(pack.Templates.Units);
        Assert.Equal("unit-1", pack.Templates.Units[0].Id);
        Assert.Equal("Knight", pack.Templates.Units[0].Name);
        Assert.Single(pack.Templates.Units[0].AbilityIds);
        Assert.Equal("ability-1", pack.Templates.Units[0].AbilityIds[0]);

        Assert.Single(pack.Templates.Abilities);
        Assert.Equal("ability-1", pack.Templates.Abilities[0].Id);
        Assert.Equal("Slash", pack.Templates.Abilities[0].Name);
        Assert.Equal("MeleeAttack", pack.Templates.Abilities[0].Category);
        Assert.Equal("effect-1", pack.Templates.Abilities[0].EffectTemplateId);
        Assert.Equal("Enemy", pack.Templates.Abilities[0].Targeting.AllowedTarget);

        Assert.Single(pack.Templates.Effects);
        Assert.Equal("effect-1", pack.Templates.Effects[0].Id);
        Assert.Equal("SlashEffect", pack.Templates.Effects[0].Name);
        Assert.Single(pack.Templates.Effects[0].ComponentTemplateIds);
        Assert.Equal("component-1", pack.Templates.Effects[0].ComponentTemplateIds[0]);

        Assert.Single(pack.Templates.EffectComponents);
        Assert.Equal("component-1", pack.Templates.EffectComponents[0].Id);
        Assert.Equal("InstantDamage", pack.Templates.EffectComponents[0].Type);

        Assert.Single(pack.GameStates);
        var gameState = pack.GameStates[0];
        Assert.Equal("scenario-1", gameState.Id);
        Assert.Equal(99, gameState.Seed);
        Assert.Equal(7, gameState.RngPosition);
        Assert.Equal(1, gameState.AttackerTeamId);
        Assert.Equal(2, gameState.DefenderTeamId);
        Assert.Equal(1, gameState.TeamToAct);
        Assert.Equal(3, gameState.AttackerTurnsTaken);
        Assert.NotNull(gameState.MapGen);
        Assert.Equal(4, gameState.MapGen.Width);
        Assert.Equal(3, gameState.MapGen.Height);
        Assert.NotNull(gameState.MapGen.TileDistribution);
        Assert.Single(gameState.MapGen.TileDistribution);
        Assert.Equal(1.0, gameState.MapGen.TileDistribution["Plain"]);
        Assert.NotNull(gameState.Units);
        Assert.Single(gameState.Units);
        Assert.Equal("unit-1", gameState.Units[0].Id);
        Assert.Equal(1, gameState.Units[0].TeamId);
        Assert.Equal(0, gameState.Units[0].Q);
        Assert.Equal(0, gameState.Units[0].R);
    }
}
