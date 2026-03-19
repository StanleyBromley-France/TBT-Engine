using Setup.Config;
using Setup.Validation.Primitives;

namespace Setup.Loading;

public interface IContentPackBuilder
{
    IContentPackTemplatesBuilder ContentPackTemplatesBuilder { get; }
    void AddGameStates(List<GameStateConfig> gameStates);
    void AddIssues(ValidationCollector issues);
}
