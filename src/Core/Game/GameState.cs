namespace Core.Game;

using Domain.Units.Instances;
using Domain.Effects.Instances;
using Map.Grid;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Instances.ReadOnly;


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

    IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>> IReadOnlyGameState.ActiveEffects =>
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

    private sealed class ReadOnlyEffectsView
        : IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>>
    {
        private readonly Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> _inner;

        // caches wrappers so it doesnt allocate a new wrapper on every access.
        // each unit gets one wrapper that points at that unit's effects dictionary.
        // wrapper is reused while the underlying dictionary instance is the same.
        private readonly Dictionary<UnitInstanceId, UnitEffectsReadOnlyView> _cache = new();

        public ReadOnlyEffectsView(
            Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> inner)
        {
            _inner = inner;
        }

        public IEnumerable<UnitInstanceId> Keys => _inner.Keys;

        // converts mutable inner dictionaries to readonly views during enumeration
        public IEnumerable<IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>> Values
        {
            get
            {
                foreach (var kv in _inner)
                    yield return GetOrCreateView(kv.Key, kv.Value);
            }
        }

        public int Count => _inner.Count;

        // converts a mutable inner dictionary to a readonly view on index access
        public IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance> this[UnitInstanceId key]
        {
            get
            {
                var dict = _inner[key];
                return GetOrCreateView(key, dict);
            }
        }

        public bool ContainsKey(UnitInstanceId key) => _inner.ContainsKey(key);

        // converts a mutable inner dictionary to a readonly view when retrieving values
        public bool TryGetValue(UnitInstanceId key, out IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance> value)
        {
            if (_inner.TryGetValue(key, out var dict))
            {
                value = GetOrCreateView(key, dict);
                return true;
            }

            value = null!;
            return false;
        }

        // converts mutable inner dictionaries to readonly views during enumeration (key/value pairs version)
        public IEnumerator<KeyValuePair<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>>> GetEnumerator()
        {
            foreach (var kv in _inner)
                yield return new KeyValuePair<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>>(
                    kv.Key,
                    GetOrCreateView(kv.Key, kv.Value));
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        // returns a cached read-only wrapper for the unit's effects dictionary 
        // if a wrapper already exists, reuse it only if it still references the same underlying dictionary instance 
        // if the dictionary has been replaced, update the wrapper to point to the new instance 
        // otherwise, create and cache a new wrapper for this unit
        private UnitEffectsReadOnlyView GetOrCreateView(
            UnitInstanceId unitId,
            Dictionary<EffectInstanceId, EffectInstance> dict)
        {
            if (_cache.TryGetValue(unitId, out var existing))
            {
                if (ReferenceEquals(existing.Inner, dict))
                    return existing;

                existing.Reset(dict);
                return existing;
            }

            var view = new UnitEffectsReadOnlyView(dict);
            _cache[unitId] = view;
            return view;
        }

        // readonly wrapper that converts mutable EffectInstance values to IReadOnlyEffectInstance for the innter dictionary
        private sealed class UnitEffectsReadOnlyView : IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>
        {
            private Dictionary<EffectInstanceId, EffectInstance> _inner;

            public UnitEffectsReadOnlyView(Dictionary<EffectInstanceId, EffectInstance> inner) => _inner = inner;

            // exposed so the outer cache can check if the underlying dictionary changed
            // and update the wrapper if the dictionary instance is replaced.
            public Dictionary<EffectInstanceId, EffectInstance> Inner => _inner;

            // updates the wrapper to point at a new dictionary instance.
            // used when the unit's effects dictionary is replaced.
            public void Reset(Dictionary<EffectInstanceId, EffectInstance> inner) => _inner = inner;

            public IEnumerable<EffectInstanceId> Keys => _inner.Keys;

            // converts mutable EffectInstance values to IReadOnlyEffectInstance
            public IEnumerable<IReadOnlyEffectInstance> Values => _inner.Values;

            public int Count => _inner.Count;

            // converts a mutable EffectInstance to IReadOnlyEffectInstance
            public IReadOnlyEffectInstance this[EffectInstanceId key] => _inner[key];

            public bool ContainsKey(EffectInstanceId key) => _inner.ContainsKey(key);

            // converts a mutable EffectInstance to IReadOnlyEffectInstance
            public bool TryGetValue(EffectInstanceId key, out IReadOnlyEffectInstance value)
            {
                if (_inner.TryGetValue(key, out var v))
                {
                    value = v;
                    return true;
                }

                value = null!;
                return false;
            }

            // converts mutable EffectInstance values to IReadOnlyEffectInstance during enumeration
            public IEnumerator<KeyValuePair<EffectInstanceId, IReadOnlyEffectInstance>> GetEnumerator()
            {
                foreach (var kv in _inner)
                    yield return new KeyValuePair<EffectInstanceId, IReadOnlyEffectInstance>(kv.Key, kv.Value);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    private sealed class ReadOnlyUnitsView : IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance>
    {
        private readonly Dictionary<UnitInstanceId, UnitInstance> _inner;

        public ReadOnlyUnitsView(Dictionary<UnitInstanceId, UnitInstance> inner) => _inner = inner;

        public IEnumerable<UnitInstanceId> Keys => _inner.Keys;

        // returns readonly unit instances during enumeration
        public IEnumerable<IReadOnlyUnitInstance> Values
        {
            get
            {
                foreach (var kv in _inner)
                    yield return kv.Value;
            }
        }

        public int Count => _inner.Count;

        // converts a mutable UnitInstance to IReadOnlyUnitInstance on index access
        public IReadOnlyUnitInstance this[UnitInstanceId key] => _inner[key];

        public bool ContainsKey(UnitInstanceId key) => _inner.ContainsKey(key);

        // converts a mutable UnitInstance to IReadOnlyUnitInstance when retrieving values
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

        // returns readonly unit instances during enumeration during enumeration (key/value pairs version)
        public IEnumerator<KeyValuePair<UnitInstanceId, IReadOnlyUnitInstance>> GetEnumerator()
        {
            foreach (var kv in _inner)
                yield return new KeyValuePair<UnitInstanceId, IReadOnlyUnitInstance>(kv.Key, kv.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
