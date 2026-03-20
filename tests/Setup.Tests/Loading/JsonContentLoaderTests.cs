namespace Setup.Tests.Loading;

using Setup.Config;
using Setup.Loading;
using Setup.Validation.Primitives;
using System.Text.Json;

public sealed class JsonContentLoaderTests
{
    [Fact]
    public void LoadFromFiles_Collects_Issue_For_Malformed_Json()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "unitTemplates.json"), "{ this is not valid json");
            WriteJson(root, "abilities.json", new List<AbilityConfig>());
            WriteJson(root, "effectTemplates.json", new List<EffectTemplateConfig>());
            WriteJson(root, "effectComponentTemplates.json", new List<EffectComponentTemplateConfig>());
            WriteJson(root, "gameStates.json", new List<GameStateConfig>());

            var pack = JsonContentLoader.LoadFromFiles(root);

            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidJsonCode && i.Path == "UnitTemplates");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public void LoadFromFiles_Collects_Issue_When_Required_File_Is_Missing()
    {
        var root = CreateTempDirectory();
        try
        {
            WriteJson(root, "unitTemplates.json", new List<UnitTemplateConfig>());

            var pack = JsonContentLoader.LoadFromFiles(root);

            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.ContentFileNotFoundCode && i.Path == "Abilities");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public void LoadFromFiles_Collects_Required_Field_Issues()
    {
        var root = CreateTempDirectory();
        try
        {
            WriteJson(root, "unitTemplates.json", new List<UnitTemplateConfig> { new() { Id = "" } });
            WriteJson(root, "abilities.json", new List<AbilityConfig> { new() { Id = "ability-1", Targeting = new TargetingRulesConfig() } });
            WriteJson(root, "effectTemplates.json", new List<EffectTemplateConfig> { new() { Id = "effect-1" } });
            WriteJson(root, "effectComponentTemplates.json", new List<EffectComponentTemplateConfig> { new() { Id = "component-1" } });
            WriteJson(root, "gameStates.json", new List<GameStateConfig> { new() { Id = "" } });

            var pack = JsonContentLoader.LoadFromFiles(root);

            Assert.True(pack.HasErrors);
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "UnitTemplates[0].Id");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "UnitTemplates[0].Name");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Name");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Category");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "Abilities[0].Targeting.AllowedTarget");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectTemplates[0].Name");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "EffectComponentTemplates[0].Type");
            Assert.Contains(pack.IssueView.Issues, i => i.Code == ContentIssueFactory.RequiredFieldCode && i.Path == "GameStates[0].Id");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static void WriteJson<T>(string directory, string fileName, T value)
    {
        var path = Path.Combine(directory, fileName);
        var json = JsonSerializer.Serialize(value);
        File.WriteAllText(path, json);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tbt-setup-loader-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
