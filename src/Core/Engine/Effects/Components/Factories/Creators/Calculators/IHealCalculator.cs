namespace Core.Engine.Effects.Components.Factories.Creators.Calculators;

using Domain.Effects.Components.Templates;
using Domain.Effects.Instances.ReadOnly;
using Mutation;
using Game;
public interface IHealCalculator
{
    int Compute(
    GameMutationContext context,
    IReadOnlyGameState state,
    IReadOnlyEffectInstance effect,
    IHealComponent componentTemplate);
}
