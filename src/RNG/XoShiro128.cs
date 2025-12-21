namespace CybeRNG_LiFE.RNG;

/// <summary>
/// Xoshiro128StarStar
/// https://docs.rs/xoshiro/latest/xoshiro/
/// "Recommended for all purposes. Excellent speed."
/// </summary>

public struct Xoshiro128StarStar : IRandomNumberGenerator
{
    private uint s0, s1, s2, s3;

    public Xoshiro128StarStar(int seed)
    {
        // 用 SplitMix32 扩展单个 seed → 4 个状态
        uint x = (uint)seed;
        s0 = SplitMix32(ref x);
        s1 = SplitMix32(ref x);
        s2 = SplitMix32(ref x);
        s3 = SplitMix32(ref x);

        // 避免全 0 状态
        if ((s0 | s1 | s2 | s3) == 0)
            s0 = 0x9E3779B9u;
    }

    // ========= 核心 =========

    public uint NextUInt()
    {
        uint result = RotL(s1 * 5u, 7) * 9u;

        uint t = s1 << 9;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = RotL(s3, 11);

        return result;
    }

    // ========= Range =========

    // int [0, max)
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

    // int [min, max)
    public int Range(int min, int max)
    {
        if (min >= max) return min;
        return min + NextInt(max - min);
    }

    // float [0, 1)
    public float NextFloat()
    {
        return (NextUInt() >> 8) * (1.0f / 16777216.0f);
    }

    // float [min, max)
    public float Range(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }

    // ========= 工具 =========

    private static uint RotL(uint x, int k)
        => (x << k) | (x >> (32 - k));

    // SplitMix32：用于 seed 扩展
    private static uint SplitMix32(ref uint x)
    {
        x += 0x9E3779B9u;
        uint z = x;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }
}
