namespace Core.Game;

using Domain.Units.Instances;
using Domain.Effects.Instances;
using Map.Grid;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;

/// <summary>
/// Represents the complete mutable state of the game.
/// This includes all units, the map, turn information, active effects, and the current RNG state.
/// </summary>
/// <remarks>
/// <para>
/// Because randomness is also stored explicitly through <see cref="RngState"/>, the entire combat 
/// simulation becomes deterministic and replayable. Given the same initial state and the same 
/// sequence of actions, the game will produce identical outcomes.
/// </para>
/// </remarks>
public sealed class GameState : IReadOnlyGameState
{
    public Map Map { get; set; } = null!;
    public List<UnitInstance> UnitInstances { get; set; } = new();
    public Dictionary<UnitInstanceId, List<EffectInstance>> ActiveEffects { get; set; } = new();
    public Turn Turn { get; set; } = null!;
    public UnitInstanceId ActiveUnitId { get; set; }
    public RngState Rng { get; set; } = null!;

    // IReadOnlyGameState projections
    IReadOnlyList<UnitInstance> IReadOnlyGameState.UnitInstances => UnitInstances;
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyList<EffectInstance>> IReadOnlyGameState.ActiveEffects =>
        new ReadOnlyEffectsView(ActiveEffects);
    public GameState(
        Map map,
        List<UnitInstance> unitInstances,
        Dictionary<UnitInstanceId, List<EffectInstance>> activeEffects,
        Turn turn,
        UnitInstanceId activeUnitId,
        RngState rng)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        UnitInstances = unitInstances ?? throw new ArgumentNullException(nameof(unitInstances));
        ActiveEffects = activeEffects ?? throw new ArgumentNullException(nameof(activeEffects));
        Turn = turn ?? throw new ArgumentNullException(nameof(turn));
        ActiveUnitId = activeUnitId;
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    private sealed class ReadOnlyEffectsView : IReadOnlyDictionary<UnitInstanceId, IReadOnlyList<EffectInstance>>
    {
        private readonly Dictionary<UnitInstanceId, List<EffectInstance>> _inner;

        public ReadOnlyEffectsView(Dictionary<UnitInstanceId, List<EffectInstance>> inner) => _inner = inner;

        public IEnumerable<UnitInstanceId> Keys => _inner.Keys;
        public IEnumerable<IReadOnlyList<EffectInstance>> Values => ToValues();
        public int Count => _inner.Count;

        public IReadOnlyList<EffectInstance> this[UnitInstanceId key] => _inner[key];

        public bool ContainsKey(UnitInstanceId key) => _inner.ContainsKey(key);
        public bool TryGetValue(UnitInstanceId key, out IReadOnlyList<EffectInstance> value)
        {
            if (_inner.TryGetValue(key, out var list))
            {
                value = list;
                return true;
            }
            value = null!;
            return false;
        }

        public IEnumerator<KeyValuePair<UnitInstanceId, IReadOnlyList<EffectInstance>>> GetEnumerator()
        {
            foreach (var kv in _inner)
                yield return new KeyValuePair<UnitInstanceId, IReadOnlyList<EffectInstance>>(kv.Key, kv.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<IReadOnlyList<EffectInstance>> ToValues()
        {
            foreach (var kv in _inner)
                yield return kv.Value;
        }
    }
}
