namespace Core.Game;

using Domain.Units.Instances;
using Domain.Effects.Instances;
using Map.Grid;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Instances.ReadOnly;

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
    public Dictionary<UnitInstanceId, UnitInstance> UnitInstances { get; set; }
    public Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> ActiveEffects { get; set; }
    public Turn Turn { get; set; }
    public UnitInstanceId ActiveUnitId { get; set; }
    public RngState Rng { get; set; } = null!;

    // IReadOnlyGameState projections

    IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance>IReadOnlyGameState.UnitInstances => 
        new ReadOnlyUnitsView(UnitInstances);

    IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, EffectInstance>> IReadOnlyGameState.ActiveEffects =>
        new ReadOnlyEffectsView(ActiveEffects);

    public GameState(
        Map map,
        Dictionary<UnitInstanceId, UnitInstance> unitInstances,
        Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> activeEffects,
        Turn turn,
        UnitInstanceId activeUnitId,
        RngState rng)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        UnitInstances = unitInstances ?? throw new ArgumentNullException(nameof(unitInstances));
        ActiveEffects = activeEffects ?? throw new ArgumentNullException(nameof(activeEffects));
        Turn = turn;
        ActiveUnitId = activeUnitId;
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    private sealed class ReadOnlyEffectsView : IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, EffectInstance>>
    {
        private readonly Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> _inner;

        public ReadOnlyEffectsView(
            Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> inner)
        {
            _inner = inner;
        }

        public IEnumerable<UnitInstanceId> Keys => _inner.Keys;

        public IEnumerable<IReadOnlyDictionary<EffectInstanceId, EffectInstance>> Values
        {
            get
            {
                foreach (var kv in _inner)
                    yield return kv.Value;
            }
        }

        public int Count => _inner.Count;

        public IReadOnlyDictionary<EffectInstanceId, EffectInstance> this[UnitInstanceId key]
            => _inner[key];

        public bool ContainsKey(UnitInstanceId key) => _inner.ContainsKey(key);

        public bool TryGetValue(
            UnitInstanceId key,
            out IReadOnlyDictionary<EffectInstanceId, EffectInstance> value)
        {
            if (_inner.TryGetValue(key, out var dict))
            {
                value = dict;
                return true;
            }

            value = null!;
            return false;
        }

        public IEnumerator<KeyValuePair<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, EffectInstance>>>
            GetEnumerator()
        {
            foreach (var kv in _inner)
                yield return new KeyValuePair<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, EffectInstance>>(
                    kv.Key,
                    kv.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class ReadOnlyUnitsView : IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance>
    {
        private readonly Dictionary<UnitInstanceId, UnitInstance> _inner;

        public ReadOnlyUnitsView(Dictionary<UnitInstanceId, UnitInstance> inner) => _inner = inner;

        public IEnumerable<UnitInstanceId> Keys => _inner.Keys;

        public IEnumerable<IReadOnlyUnitInstance> Values
        {
            get
            {
                foreach (var kv in _inner)
                    yield return kv.Value;
            }
        }

        public int Count => _inner.Count;

        public IReadOnlyUnitInstance this[UnitInstanceId key] => _inner[key];

        public bool ContainsKey(UnitInstanceId key) => _inner.ContainsKey(key);

        public bool TryGetValue(UnitInstanceId key, out IReadOnlyUnitInstance value)
        {
            if (_inner.TryGetValue(key, out var unit))
            {
                value = unit;
                return true;
            }

            value = null!;
            return false;
        }

        public IEnumerator<KeyValuePair<UnitInstanceId, IReadOnlyUnitInstance>> GetEnumerator()
        {
            foreach (var kv in _inner)
                yield return new KeyValuePair<UnitInstanceId, IReadOnlyUnitInstance>(kv.Key, kv.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
