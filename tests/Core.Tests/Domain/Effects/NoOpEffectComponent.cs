using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Core.Game;

namespace Core.Tests.Domain.Effects;

internal sealed class NoOpEffectComponent : EffectComponentTemplate
{
    public NoOpEffectComponent(EffectComponentTemplateId id) : base(id)
    {
    }

    public GameState ApplyInitial(GameState state, string sourceUnitId, string targetUnitId)
        => state;

    public GameState Tick(GameState state, string sourceUnitId, string targetUnitId)
        => state;
}
