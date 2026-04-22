namespace Core.Random;

/// <summary>
/// Derives deterministic domain-specific seeds from a shared top-level seed.
/// </summary>
public static class SeedDeriver
{
    public static int Derive(int seed, int salt)
    {
        unchecked
        {
            uint value = (uint)seed;
            value ^= (uint)salt + 0x9E3779B9u + (value << 6) + (value >> 2);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (int)value;
        }
    }
}
