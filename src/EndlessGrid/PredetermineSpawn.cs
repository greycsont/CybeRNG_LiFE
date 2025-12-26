using System;
using System.Text.RegularExpressions;
using CybeRNG_LiFE.RNG;


namespace CybeRNG_LiFE;

public static class PredetermineManager
{
    public static void RoundCurrentPatternByWave(EndlessGrid endlessGrid)
    {
        // 换想法了，直接把这个函数插到for循环里
        // The game start from wave 0 isn't it?
        var currentWave = 0;

        for (int i = 0; i < endlessGrid.startWave - 1; i++)
        {
            endlessGrid.currentPatternNum++;
            RandomManager.FreshRNG();
            if (endlessGrid.currentPatternNum >= endlessGrid.CurrentPatternPool.Length)
                endlessGrid.ShuffleDecks();

            currentWave++;

            PredetermineSpawn(endlessGrid, currentWave);
        }
    }
    public static void PredetermineSpawn(EndlessGrid endlessGrid, int currentWave)
    {
        endlessGrid.massAntiBuffer += PredetermineHideous(
            endlessGrid.CurrentPatternPool[endlessGrid.currentPatternNum],
            endlessGrid.massAntiBuffer,
            endlessGrid.points,
            currentWave);
    }

    private static int PredetermineHideous(ArenaPattern currentPattern, int massAntiBuffer, int points, int currentWave)
    {
        if (currentPattern == null)
            return 0;

        // pattern is a 16x16 matrix
        // seperate the rows and run through each rows
        string[] rows = currentPattern.prefabs.Split('\n');
        if (rows.Length != 16)
            return 0;
        
        int count = 0;
        
        var hideousMasses = 0;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].Length != 16) continue;
            
            for (int j = 0; j < rows[i].Length; j++)
            {
                if (rows[i][j] == 'H')
                {
                    if (massAntiBuffer == 0 && 
                        currentWave >= (hideousMasses + 1) * 10 && 
                        points > 70)
                    {
                        count++;
                    }
                }
            }
        }
        
        if(count > 0)
            return count * 2;
        else
            return -1;
    }
}