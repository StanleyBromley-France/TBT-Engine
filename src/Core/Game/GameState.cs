using System.Collections.Immutable;

namespace Core.Game;

using Units.Instances;
using Effects.Instances;

/// <summary>
/// Represents the complete immutable state of the game at a specific moment in time.
/// This includes all units, the map, turn information, active effects, and the current RNG state.
/// Every game update produces a new <see cref="GameState"/>, ensuring pure and deterministic progression.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GameState"/> is a value object. It contains no behavior beyond construction and 
/// should never be mutated after creation. All modifications to the game (movement, ability use, 
/// effect application, turn progression, etc.) must produce a new <see cref="GameState"/> instance.
/// </para>
/// <para>
/// Because randomness is also stored explicitly through <see cref="RngState"/>, the entire combat 
/// simulation becomes deterministic and replayable. Given the same initial state and the same 
/// sequence of actions, the game will produce identical outcomes.
/// </para>
/// </remarks>
public sealed class GameState
{
    public Map Map { get; }
    public ImmutableList<UnitInstance> UnitInstances { get; }
    public Turn Turn { get; }
    public string ActiveUnitId { get; }
    public RngState Rng { get; }

    // unitId → applied effects
    public ImmutableDictionary<string, ImmutableList<EffectInstance>> ActiveEffects { get; }

    public string Hash { get; }

    public GameState(
        Map map,
        ImmutableList<UnitInstance> unitInstances,
        Turn turn,
        string activeUnitId,
        RngState rng,
        ImmutableDictionary<string, ImmutableList<EffectInstance>> activeEffects,
        string hash = "")
    {
        Map = map;
        UnitInstances = unitInstances;
        Turn = turn;
        ActiveUnitId = activeUnitId;
        Rng = rng;
        ActiveEffects = activeEffects;
        Hash = hash;
    }
}

// temp forward declaration
public sealed class Map { }

