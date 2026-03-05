namespace Core.Engine.Mutation.Mutators;

/// <summary>
/// Mutation-layer API for managing random number genaration using RngState.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="DeterministicRng"/> and ensures that all RNG state changes
/// are applied through <see cref="GameMutationContext"/>.
/// </para>
/// <para>
/// Each roll advances the stored RNG state inside <see cref="Core.Game.GameState"/>,
/// guaranteeing deterministic and replayable outcomes.
/// </para>
/// Intended to be used exclusively by engine rules and effects through
/// the mutation pipeline.
/// </remarks>
public interface IRngMutator
{
    int RollRandom(int exclusiveMax);

    int RollRandom(int inclusiveMin, int exclusiveMax);
}