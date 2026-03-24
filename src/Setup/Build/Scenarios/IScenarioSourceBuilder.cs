namespace Setup.Build.Scenarios;

using Setup.Loading.Scenarios;
using Setup.Validation.Primitives;

public interface IScenarioSourceBuilder
{
    IScenarioSource Build(
        LoadedScenarioContent content,
        ContentValidationMode validationMode);
}
