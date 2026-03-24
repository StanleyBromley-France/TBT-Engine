namespace Setup.Build.Scenarios;

using Setup.Validation.Primitives;

public interface IScenarioSource
{
    IReadOnlyList<string> GameStateIds { get; }

    IContentIssueView IssueView { get; }

    bool HasErrors { get; }

    ScenarioResult Build(
        string gameStateId,
        ContentValidationMode validationMode);
}
