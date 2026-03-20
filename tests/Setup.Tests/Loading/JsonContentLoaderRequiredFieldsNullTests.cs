namespace Setup.Tests.Loading;

using Setup.Loading;
using Setup.Validation.Primitives;

public sealed class JsonContentLoaderRequiredFieldsNullTests
{
    [Fact]
    public void LoadFromFiles_With_Template_Files_Catches_Required_Null_Or_Empty_For_All_Types()
    {
        var pack = JsonContentLoader.LoadFromFiles(LoaderFixturePaths.Template);

        Assert.True(pack.HasErrors);

        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "UnitTemplates[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "UnitTemplates[0].Name");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode && i.Path == "UnitTemplates[0].AbilityIds");

        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Name");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Category");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].EffectTemplateId");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Targeting.AllowedTarget");

        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectTemplates[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectTemplates[0].Name");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode && i.Path == "EffectTemplates[0].ComponentTemplateIds");

        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectComponentTemplates[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectComponentTemplates[0].Type");

        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "GameStates[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode && i.Path == "GameStates[0].MapGen.TileDistribution");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode && i.Path == "GameStates[0].Units");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullCollectionCode && i.Path == "GameStates[1].MapGen.TileDistribution");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "GameStates[1].Units[0].Id");
        Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.NullItemCode && i.Path == "GameStates[1].Units[1]");
    }
}
