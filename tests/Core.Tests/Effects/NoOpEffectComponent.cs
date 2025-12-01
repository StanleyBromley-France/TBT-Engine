using Core.Effects.Templates;
using Core.Game;

namespace Core.Tests.Effects;

internal sealed class NoOpEffectComponent : EffectComponentTemplate
{
    public NoOpEffectComponent(string id) : base(id)
    {
    }

    public override GameState ApplyInitial(GameState state, string sourceUnitId, string targetUnitId)
        => state;

    public override GameState Tick(GameState state, string sourceUnitId, string targetUnitId)
        => state;
}
