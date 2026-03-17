namespace Agents.Mcts.Hashing;

public sealed partial class GameStateHasher
{
    private struct HashBuilder
    {
        // FNV-1a style mixing provides a compact deterministic in-memory key
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _value;

        public void Add(bool value) => Add(value ? 1 : 0);

        public void Add(int value)
        {
            EnsureInitialized();

            unchecked
            {
                // Mixes the integer one byte at a time so the hash depends on the
                // full value instead of only its low bits.
                _value ^= (byte)value;
                _value *= Prime;

                _value ^= (byte)(value >> 8);
                _value *= Prime;

                _value ^= (byte)(value >> 16);
                _value *= Prime;

                _value ^= (byte)(value >> 24);
                _value *= Prime;
            }
        }

        public void Add(string? value)
        {
            EnsureInitialized();

            if (value is null)
            {
                Add(-1);
                return;
            }

            Add(value.Length);

            unchecked
            {
                // Strings are mixed as UTF-16 code units to match how .NET stores chars
                foreach (var character in value)
                {
                    _value ^= (byte)character;
                    _value *= Prime;

                    _value ^= (byte)(character >> 8);
                    _value *= Prime;
                }
            }
        }

        public GameStateKey ToKey()
        {
            EnsureInitialized();
            return new GameStateKey(_value);
        }

        private void EnsureInitialized()
        {
            if (_value == 0)
                _value = OffsetBasis;
        }
    }
}
