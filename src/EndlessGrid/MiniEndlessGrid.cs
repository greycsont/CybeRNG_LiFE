using System;

namespace CybeRNG_LiFE;

public struct MiniEndlessGrid
{
    public int massAntiBuffer;
    public float uncommonAntiBuffer;
    public int specialAntiBuffer;
    public int meleePositionsCount;
    public int projectilePositionsCount;
    public int hideousMassPositionCount;
    public int points;
    public MiniEndlessGrid(int mass, float uncommon, int special, int points)
    {
        this.massAntiBuffer = mass;
        this.uncommonAntiBuffer = uncommon;
        this.specialAntiBuffer = special;
        this.points = points;
    }
    public void Clear()
    {
        massAntiBuffer = 0;
        uncommonAntiBuffer = 0;
        specialAntiBuffer = 0;
    }
    public void SetPositionCount(int m, int p, int h)
    {
        this.meleePositionsCount = m;
        this.projectilePositionsCount = p;
        this.hideousMassPositionCount = h;
    }
}