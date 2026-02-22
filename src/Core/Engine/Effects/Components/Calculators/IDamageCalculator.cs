namespace Core.Engine.Effects.Components.Calculators;

using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Game;

public interface IDamageCalculator
{
    int Compute(
        GameMutationContext context,
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect,
        IDamageComponent componentTemplate);
}