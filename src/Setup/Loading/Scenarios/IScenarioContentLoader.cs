namespace Setup.Loading.Scenarios;

public interface IScenarioContentLoader
{
    LoadedScenarioContent Load(string contentPath);
}
