namespace Core.Engine.Effects;

using Core.Engine.Mutation;
using Core.Game;

public interface IEffectManager
{
    void ApplyOrStackEffect(GameMutationContext context, IReadOnlyGameState state, EffectApplicationRequest request);
    void TickAll(GameMutationContext context, IReadOnlyGameState state);
}
