using System;
using System.Linq;
using UnityEngine;
using CybeRNG_LiFE.RNG;


namespace CybeRNG_LiFE;

public static class MiniEndlessGridManager
{
    // Greatest Shitpost ever

    public static MiniEndlessGrid miniEndlessGrid;


    /// <summary>
    /// Round to the next pattern and predetermine spawns
    /// Automatically Rounding pattern with related RNG
    /// PreCalculate the Hideous mass, Uncommon and Special enmeys and cout them into the AntiBuffers
    /// </summary>
    public static void RoundCurrentPattern(EndlessGrid endlessGrid)
    {
        endlessGrid.currentPatternNum++;
        RandomManager.FreshRNG();
        if (endlessGrid.currentPatternNum >= endlessGrid.CurrentPatternPool.Length)
            endlessGrid.ShuffleDecks();

        miniEndlessGrid.currentWave++;

        PredetermineSpawn(endlessGrid);
    }
    public static void AddAntiBufferToEndlessGrid(EndlessGrid endlessGrid)
    {
        endlessGrid.massAntiBuffer += miniEndlessGrid.massAntiBuffer;
        endlessGrid.uncommonAntiBuffer += miniEndlessGrid.uncommonAntiBuffer;
        endlessGrid.specialAntiBuffer += miniEndlessGrid.specialAntiBuffer;
        Plugin.Logger.LogDebug($"Added antibuffer to endlessgrid: H: {miniEndlessGrid.massAntiBuffer}, U: {miniEndlessGrid.uncommonAntiBuffer}, S: {miniEndlessGrid.specialAntiBuffer}");
    }

    public static void InitializeMiniEndlessGrid()
    {
        miniEndlessGrid = new MiniEndlessGrid(0, 0f, 0, 0);
    }

    private static void PredetermineSpawn(EndlessGrid endlessGrid)
    {
        miniEndlessGrid.points = endlessGrid.maxPoints;

        var (m, p, h) = ParsingPattern(endlessGrid.CurrentPatternPool[endlessGrid.currentPatternNum]);

        miniEndlessGrid.SetPositionCount(m, p, h);

        miniEndlessGrid.PredetermineSpawn(endlessGrid);
    }


    private static (int Mcount, int Pcount, int Hcount) ParsingPattern(ArenaPattern currentPattern)
    {
        if (currentPattern == null || string.IsNullOrEmpty(currentPattern.prefabs))
        {
            Plugin.Logger.LogError("Fuck Parser");
            return (0, 0, 0);
        }
        string[] rows = currentPattern.prefabs.Split('\n');

        if (rows.Length != 16) return (0, 0, 0);

        int meleePositionCount = rows.Where(r => r.Length == 16)
                .Sum(r => r.Count(c => c == 'n'));
        int projectilePositionsCount = rows.Where(r => r.Length == 16)
                    .Sum(r => r.Count(c => c == 'p'));
        int hideousMassPositionCount = rows.Where(r => r.Length == 16)
                    .Sum(r => r.Count(c => c == 'H'));

        return (meleePositionCount, projectilePositionsCount, hideousMassPositionCount);
    }
}