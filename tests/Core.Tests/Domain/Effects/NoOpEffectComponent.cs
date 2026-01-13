using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Core.Game;

namespace Core.Tests.Domain.Effects;

internal sealed class NoOpEffectComponent : EffectComponentTemplate
{
    public NoOpEffectComponent(EffectComponentTemplateId id) : base(id)
    {
    }

    public override GameState ApplyInitial(GameState state, string sourceUnitId, string targetUnitId)
        => state;

    public override GameState Tick(GameState state, string sourceUnitId, string targetUnitId)
        => state;
}
