using System;
using System.Linq;
using CybeRNG_LiFE.RNG;
using Discord;


namespace CybeRNG_LiFE;

public static class PredetermineManager
{
    // Greatest Shitpost ever

    public static MiniEndlessGrid miniEndlessGrid = new MiniEndlessGrid(0, 0f, 0, 0);


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

        miniEndlessGrid.ClearForNextWave();
    }

    private static void PredetermineSpawn(EndlessGrid endlessGrid)
    {
        miniEndlessGrid.points = endlessGrid.maxPoints;

        var (m, p, h) = ParsingPattern(endlessGrid.CurrentPatternPool[endlessGrid.currentPatternNum]);
        miniEndlessGrid.SetPositionCount(m, p, h);

        var hideousMassCount = PredetermineHideous(miniEndlessGrid);

        var delta = hideousMassCount > 0 ? hideousMassCount * 2 : -1;

        miniEndlessGrid.massAntiBuffer = Math.Max(0, miniEndlessGrid.massAntiBuffer += delta);

        //var (uncommonCount, specialCount) = PredetermineUncommonAndSpecial();
        //miniEndlessGrid.uncommonAntiBuffer += uncommonCount;
        //miniEndlessGrid.specialAntiBuffer += specialCount;
        Plugin.Logger.LogInfo($"[PredetermineSpawn] Wave {miniEndlessGrid.currentWave}: Hideous Masses Predetermined: {hideousMassCount}, Mass AntiBuffer: {miniEndlessGrid.massAntiBuffer}");
    }

    private static int PredetermineHideous(MiniEndlessGrid grid)
    {
        var Hcount = miniEndlessGrid.hideousMassPositionCount;
        Plugin.Logger.LogDebug($"Hcount: {Hcount}");
        int hideousMassesCount = 0;

        for (int i = 0; i < Hcount; i++)
        {
            if (grid.massAntiBuffer == 0 &&
                grid.currentWave >= (hideousMassesCount + 1) * 10 &&
                grid.points > 70)
            {
                hideousMassesCount++;
                grid.points -= 45;
            }
        }

        return hideousMassesCount;
    }

    private static (float uncommonCount, int specialCount) PredetermineUncommonAndSpecial(int hideousMassCount,
                                                                                          MiniEndlessGrid miniEndlessGrid,
                                                                                          EndlessGrid endlessGrid)
    {
        float uncommonCount = 0f;
        int specialCount = 0;

        // 1. calculate baseSpawnPoint
        miniEndlessGrid.baseSpawnPoint = miniEndlessGrid.currentWave / 10; // currentWave DIV 10
        miniEndlessGrid.baseSpawnPoint -= hideousMassCount;

        // 如果没有可用数量，直接返回
        if (miniEndlessGrid.baseSpawnPoint <= 0) return (0f, 0);

        // 3. Predetermine Uncommon
        if (miniEndlessGrid.uncommonAntiBuffer < 1f && miniEndlessGrid.baseSpawnPoint > 0)
        {
            // It need to consume a RNG value so don't use lambda expression here
            var numberOfFirstUncommon = RandomManager.enemySpawnRNG.Range(0, miniEndlessGrid.currentWave / 10 + 1);
            if (miniEndlessGrid.uncommonAntiBuffer <= 0.5f && numberOfFirstUncommon < 1)
                numberOfFirstUncommon = 1;

            if (miniEndlessGrid.massAntiBuffer < 1f && miniEndlessGrid.meleePositionsCount > 0)
            {
                int firstUncommonIndex = RandomManager.enemySpawnRNG.Range(0, endlessGrid.prefabs.uncommonEnemies.Length);
                int secondUncommonIndex = RandomManager.enemySpawnRNG.Range(0, endlessGrid.prefabs.uncommonEnemies.Length);
                int numberOfSecondUncommon = 0;

                while (firstUncommonIndex >= 0 && miniEndlessGrid.currentWave < endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].spawnWave)
                    firstUncommonIndex--;

                while (secondUncommonIndex >= 0 && (miniEndlessGrid.currentWave < endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].spawnWave || secondUncommonIndex == firstUncommonIndex))
                {
                    if (secondUncommonIndex == 0)
                    {
                        numberOfSecondUncommon = -1;
                        break;
                    }
                    secondUncommonIndex--;
                }
                if (firstUncommonIndex >= 0)
                {
                    if (miniEndlessGrid.currentWave > 16)
                    {
                        if (miniEndlessGrid.currentWave < 25)
                        {
                            numberOfFirstUncommon++;
                        }
                        else if (numberOfSecondUncommon != -1)
                        {
                            numberOfSecondUncommon = numberOfFirstUncommon;
                        }
                    }

                    bool isFirstUncommonSpawnSuccessfully = DetermineUncommonSpawnPosition(firstUncommonIndex, numberOfFirstUncommon, miniEndlessGrid, endlessGrid);
                    bool isSecondUncommonSpawnSuccessfully = false;
                    if (numberOfSecondUncommon > 0)
                        isSecondUncommonSpawnSuccessfully = DetermineUncommonSpawnPosition(secondUncommonIndex, numberOfSecondUncommon, miniEndlessGrid, endlessGrid);                    
                    if (isFirstUncommonSpawnSuccessfully || isSecondUncommonSpawnSuccessfully)
                    {
                        if (miniEndlessGrid.uncommonAntiBuffer < 0f)
                        {
                            miniEndlessGrid.uncommonAntiBuffer = 0f;
                        }
                        if (isFirstUncommonSpawnSuccessfully)
                        {
                            miniEndlessGrid.uncommonAntiBuffer += (endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Stalker 
                                                                   || endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                        }
                        if (isSecondUncommonSpawnSuccessfully)
                        {
                            miniEndlessGrid.uncommonAntiBuffer += (endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Stalker 
                                                                   || endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                        }
                        miniEndlessGrid.baseSpawnPoint -= (!(isFirstUncommonSpawnSuccessfully && isSecondUncommonSpawnSuccessfully)) ? 1 : 2;
                    }
                }
            }
        }
        else
        {
            miniEndlessGrid.uncommonAntiBuffer--;
        }
        /*
        if (currentWave > 15)
        {
            var isSpawnSpecialSuccessfully = false;
            if (miniEndlessGrid.specialAntiBuffer <= 0 && baseSpawnPoint > 0)
            {
                int numberOfSpecial = RandomManager.enemySpawnRNG.Range(0, baseSpawnPoint + 1);
                if (miniEndlessGrid.specialAntiBuffer <= -2 && numberOfSpecial < 1)
                {
                    numberOfSpecial = 1;
                }
                if (numberOfSpecial > 0 && miniEndlessGrid.meleePositionsCount > 0)
                {
                    for (int i = 0; i < numberOfSpecial; i++)
                    {
                        int indexOfSpecial = RandomManager.enemySpawnRNG.Range(0, endlessGridPerfab.specialEnemies.Length);
                        int indexOfEnemyType = 
                    }
                }
            }
        }*/

        return (uncommonCount, specialCount);
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
    
    private static bool DetermineUncommonSpawnPosition(int target, int amount, MiniEndlessGrid miniEndlessGrid, EndlessGrid endlessGrid)
    {
        amount = endlessGrid.CapUncommonsAmount(target, amount);
        var spawnResult = false;
        for (int i = 0; i < amount; i++)
        {
            var endlessEnemy = endlessGrid.prefabs.uncommonEnemies[target];
            bool spawnOnProjectile = endlessEnemy.enemyType != EnemyType.Stalker && endlessEnemy.enemyType != EnemyType.Guttertank && RandomManager.enemySpawnRNG.Range(0f, 1f) > 0.5f;
            if (spawnOnProjectile && miniEndlessGrid.projectilePositionsCount <= 0)
            {
                spawnOnProjectile = false;
            }
            if (miniEndlessGrid.meleePositionsCount <= 0)
            {
                break;
            }
            // IDK why but the original did that
            var indexOfEnemyType = miniEndlessGrid.GetIndexOfEnemyType(endlessEnemy.enemyType);
            int extraPointReduced = endlessEnemy.costIncreasePerSpawn * miniEndlessGrid.spawnedEnemyTypes[target].amount;
            bool isSpawnRadiantSuccessfully = miniEndlessGrid.DetermineSpawnRadiant(endlessEnemy, indexOfEnemyType);
            // I think it can be write as | isSpawnRadiantSuccessfully ? 3 : 1 |
            miniEndlessGrid.points -= endlessEnemy.spawnCost * ((!isSpawnRadiantSuccessfully) ? 1 : 3) + extraPointReduced;
            miniEndlessGrid.spawnedEnemyTypes[indexOfEnemyType].amount++;
            if (spawnOnProjectile)
            {
                miniEndlessGrid.projectilePositionsCount--;
            }
            else
            {
                miniEndlessGrid.meleePositionsCount--;
            }
            spawnResult = true;
            if (isSpawnRadiantSuccessfully)
            {
                amount -= 2;
            }
        }
        return spawnResult;
    }
}