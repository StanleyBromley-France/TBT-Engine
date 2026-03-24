namespace Setup.ScenarioSetup;

using Setup.Build.Scenarios;
using Setup.Loading.Scenarios;
using Setup.Validation.Primitives;

public sealed class ScenarioSetup : IScenarioSetup
{
    private readonly IScenarioContentLoader _contentLoader;
    private readonly IScenarioSourceBuilder _sourceBuilder;

    public ScenarioSetup()
        : this(new ScenarioContentLoader(), new ScenarioSourceBuilder())
    {
    }

    public ScenarioSetup(
        IScenarioContentLoader contentLoader,
        IScenarioSourceBuilder sourceBuilder)
    {
        _contentLoader = contentLoader ?? throw new ArgumentNullException(nameof(contentLoader));
        _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
    }

    public IScenarioSource Load(
        string contentPath,
        ContentValidationMode validationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentPath);

        var content = _contentLoader.Load(contentPath);
        return _sourceBuilder.Build(content, validationMode);
    }

    public ScenarioResult Create(
        IScenarioSource content,
        string gameStateId,
        ContentValidationMode validationMode)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameStateId);

        return content.Build(gameStateId, validationMode);
    }
}
