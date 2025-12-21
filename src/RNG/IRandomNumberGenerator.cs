namespace CybeRNG_LiFE.RNG;

public interface IRandomNumberGenerator
{
    public int Range(int min, int max);
    public float Range(float min, float max);
    public uint NextUInt();
}