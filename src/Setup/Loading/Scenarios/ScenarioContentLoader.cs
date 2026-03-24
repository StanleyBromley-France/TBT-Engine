namespace Setup.Loading.Scenarios;

using Setup.Loading;

public sealed class ScenarioContentLoader : IScenarioContentLoader
{
    public LoadedScenarioContent Load(string contentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentPath);

        var contentPack = JsonContentLoader.LoadFromFiles(contentPath);
        return new LoadedScenarioContent(contentPack, contentPack.IssueView);
    }
}
