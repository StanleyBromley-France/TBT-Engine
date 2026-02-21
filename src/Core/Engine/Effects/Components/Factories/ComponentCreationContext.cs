namespace Core.Engine.Effects.Components.Factories;

using Core.Engine.Mutation;
using Core.Game;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Engine.Mutation.Mutators;

public sealed class ComponentCreationContext
{
    public RngMutator Rng { get; }
    public IReadOnlyGameState State { get; }
    public IReadOnlyEffectInstance Effect { get; }

    public ComponentCreationContext(
        RngMutator rng,
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect)
    {
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }
}