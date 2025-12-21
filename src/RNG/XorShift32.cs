namespace CybeRNG_LiFE.RNG;

/// <summary>
/// XorShift32
/// https://en.wikipedia.org/wiki/Xorshift
/// </summary>
public struct XorShift32(int seed) : IRandomNumberGenerator
{
    private uint state = (uint)seed == 0 ? 0x6D2B79F5 : (uint)seed;  // vibe coding

    public uint NextUInt()
    {
        uint x = state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        state = x;
        return x;
    }

    // int[0,max)
    public int NextInt(int max)
    {
        if (max <= 0) return 0;
        uint x;
        uint limit = uint.MaxValue - uint.MaxValue % (uint)max;
        do
        {
            x = NextUInt();
        } while (x >= limit);
        return (int)(x % (uint)max);
    }

    // int[min,max)
    public int Range(int min, int max)
    {
        if (min >= max) return min;
        return min + NextInt(max - min);
    }

    // float[0,1)
    public float NextFloat()
    {
        return (NextUInt() >> 8) * (1.0f / 16777216.0f); // ((x^32-1) / 2^8 / 2^24) => [0,1) 
    }

    // float[min,max)
    public float Range(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }
}

