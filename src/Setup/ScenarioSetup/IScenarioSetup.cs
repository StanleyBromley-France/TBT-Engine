namespace Setup.ScenarioSetup;

using Setup.Build.Scenarios;
using Setup.Validation.Primitives;

public interface IScenarioSetup
{
    IScenarioSource Load(
        string contentPath,
        ContentValidationMode validationMode);

    ScenarioResult Create(
        IScenarioSource content,
        string gameStateId,
        ContentValidationMode validationMode);
}
