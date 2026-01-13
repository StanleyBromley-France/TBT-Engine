namespace Core.Domain.Effects.Templates;

using Core.Domain.Types;
using Core.Game;

/// <summary>
/// Serves as the base definition for an effect component, providing
/// identifiers and lifecycle behavior hooks for effect instances
/// </summary>
public abstract class EffectComponentTemplate
{
    public EffectComponentTemplateId Id { get; }

    protected EffectComponentTemplate(EffectComponentTemplateId id)
    {
        Id = id;
    }

    public abstract GameState ApplyInitial(GameState state, string sourceUnitId, string targetUnitId);

    public abstract GameState Tick(GameState state, string sourceUnitId, string targetUnitId);
}

