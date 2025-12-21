using GameConsole.pcon;

namespace CybeRNG_LiFE.RNG;

/// <summary>
/// PCG32 (PCG-XSH-RR 32-bit output)
/// https://en.wikipedia.org/wiki/Permuted_congruential_generator
/// </summary>
public struct PCG32 : IRandomNumberGenerator
{
    private ulong state;
    private readonly ulong increment; // 必须是奇数

    // two black magic number
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Baseincrement = 1442695040888963407UL;

    /// <summary>
    /// seed: 任意 int
    /// stream: 用来区分不同 RNG 流
    /// </summary>
    public PCG32(int seed, int stream = 1)
    {
        state = 0UL;
        increment = (Baseincrement + ((ulong)stream << 1)) | 1UL;
        state += (uint)seed;
        NextUInt();
    }

    /// <summary>
    /// 生成 uint32
    /// </summary>
    public uint NextUInt()
    {
        ulong oldState = state;
        state = oldState * Multiplier + increment;

        // ========= XSH RR =========
        // XSH (xorshift high bits)
        uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);

        // RR (rotate right)
        int rot = (int)(oldState >> 59);

        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    // int [0, max)
    public int NextInt(int max)
    {
        if (max <= 0) return 0;

        uint bound = (uint)max;
        uint threshold = (uint)(-bound) % bound;

        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold)
                return (int)(r % bound);
        }
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
        // 取 24 位有效随机数 → float mantissa
        return (NextUInt() >> 8) * (1.0f / 16777216.0f); // ((x^32-1) / 2^8 / 2^24) => [0,1) 
    }

    // float [min, max)
    public float Range(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }
}
