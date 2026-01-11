using Core.Effects.Templates;
using Core.Game;
using Core.Types;

namespace Core.Tests.Effects;

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
