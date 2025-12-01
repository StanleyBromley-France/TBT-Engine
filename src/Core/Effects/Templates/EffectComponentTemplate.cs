using Core.Game;

namespace Core.Effects.Templates;

/// <summary>
/// Serves as the base definition for an effect component, providing
/// identifiers and lifecycle behavior hooks for effect instances
/// </summary>
public abstract class EffectComponentTemplate
{
    public string Id { get; }

    protected EffectComponentTemplate(string id)
    {
        Id = id;
    }

    public abstract GameState ApplyInitial(GameState state, string sourceUnitId, string targetUnitId);

    public abstract GameState Tick(GameState state, string sourceUnitId, string targetUnitId);
}

