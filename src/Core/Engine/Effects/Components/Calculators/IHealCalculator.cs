namespace Core.Engine.Effects.Components.Calculators;

using Domain.Effects.Components.Templates;
using Domain.Effects.Instances.ReadOnly;
using Mutation;
using Game;
using Core.Game.State.ReadOnly;

public interface IHealCalculator
{
    int Compute(
    GameMutationContext context,
    IReadOnlyGameState state,
    IReadOnlyEffectInstance effect,
    IHealComponent componentTemplate);
}
