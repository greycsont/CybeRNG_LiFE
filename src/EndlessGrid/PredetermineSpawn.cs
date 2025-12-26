using System;
using System.Text.RegularExpressions;
using System.Linq;
using CybeRNG_LiFE.RNG;


namespace CybeRNG_LiFE;

public static class PredetermineManager
{
    // Greatest Shitpost ever
    public static int currentWave = 0;

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

        currentWave++;

        PredetermineSpawn(endlessGrid, currentWave);
    }
    public static void AddAntiBufferToEndlessGrid(EndlessGrid endlessGrid)
    {
        endlessGrid.massAntiBuffer += miniEndlessGrid.massAntiBuffer;
        endlessGrid.uncommonAntiBuffer += miniEndlessGrid.uncommonAntiBuffer;
        endlessGrid.specialAntiBuffer += miniEndlessGrid.specialAntiBuffer;

        miniEndlessGrid.Clear();
    }
    
    private static void PredetermineSpawn(EndlessGrid endlessGrid, int currentWave)
    {
        miniEndlessGrid.points = endlessGrid.maxPoints;

        var (m,p,h) = ParsingPattern(endlessGrid.CurrentPatternPool[endlessGrid.currentPatternNum]);
        miniEndlessGrid.SetPositionCount(m,p,h);

        var hideousMassCount = PredetermineHideous(miniEndlessGrid, currentWave);

        var delta = hideousMassCount > 0 ? hideousMassCount * 2 : -1;

        miniEndlessGrid.massAntiBuffer = Math.Max(0, miniEndlessGrid.massAntiBuffer += delta);
        
        //var (uncommonCount, specialCount) = PredetermineUncommonAndSpecial();
        //miniEndlessGrid.uncommonAntiBuffer += uncommonCount;
        //miniEndlessGrid.specialAntiBuffer += specialCount;
        Plugin.Logger.LogInfo($"[PredetermineSpawn] Wave {currentWave}: Hideous Masses Predetermined: {hideousMassCount}, Mass AntiBuffer: {miniEndlessGrid.massAntiBuffer}");
    }

    private static int PredetermineHideous(MiniEndlessGrid grid, 
                                           int currentWave)
    {
        var Hcount = miniEndlessGrid.hideousMassPositionCount;
        Plugin.Logger.LogDebug($"Hcount: {Hcount}");
        int hideousMassesCount = 0;

        for (int i = 0; i < Hcount; i++)
        {
            if (grid.massAntiBuffer == 0 && 
                currentWave >= (hideousMassesCount + 1) * 10 && 
                grid.points > 70)
            {
                hideousMassesCount++;
                grid.points -= 45;
            }
        }
        
        return hideousMassesCount;
    }

    private static (float uncommonCount, int specialCount) PredetermineUncommonAndSpecial(int hideousMassCount,
                                                                                          int currentWave,
                                                                                          MiniEndlessGrid miniEndlessGrid,
                                                                                          PrefabDatabase endlessGridPerfab)
    {
        float uncommonCount = 0f;
        int specialCount = 0;
        
        // 1. calculate baseSpawnPoint
        int baseSpawnPoint = currentWave / 10; // currentWave DIV 10
        baseSpawnPoint -= hideousMassCount;
        
        // 如果没有可用数量，直接返回
        if (baseSpawnPoint <= 0) return (0f, 0);
        
        // 3. Predetermine Uncommon
        if (miniEndlessGrid.uncommonAntiBuffer < 1f && baseSpawnPoint > 0)
        {
            // It need to consume a RNG value so don't use lambda expression here
            var numberOfFirstUncommon = RandomManager.enemySpawnRNG.Range(0, currentWave / 10 + 1);
            if (miniEndlessGrid.uncommonAntiBuffer <= 0.5f && numberOfFirstUncommon < 1)
                numberOfFirstUncommon = 1;
            
            if (miniEndlessGrid.massAntiBuffer < 1f && miniEndlessGrid.meleePositionsCount > 0)
            {
                int firstUncommonIndex = RandomManager.enemySpawnRNG.Range(0,endlessGridPerfab.uncommonEnemies.Length);
                int secondUncommonIndex = RandomManager.enemySpawnRNG.Range(0,endlessGridPerfab.uncommonEnemies.Length);
                int numberOfSecondUncommon = 0;

                while (firstUncommonIndex >= 0 && currentWave < endlessGridPerfab.uncommonEnemies[firstUncommonIndex].spawnWave)
                    firstUncommonIndex--;
            
                while (secondUncommonIndex >= 0 && (currentWave < endlessGridPerfab.uncommonEnemies[secondUncommonIndex].spawnWave || secondUncommonIndex == firstUncommonIndex))
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
                    if (currentWave > 16)
                    {
                        if (currentWave < 25)
                        {
                            numberOfFirstUncommon++;
                        }
                        else if (numberOfSecondUncommon != -1)
                        {
                            numberOfSecondUncommon = numberOfFirstUncommon;
                        }
                    }
                    bool flag = miniEndlessGrid.meleePositionsCount > 0;
                    miniEndlessGrid.meleePositionsCount = Math.Max(0, miniEndlessGrid.meleePositionsCount - numberOfFirstUncommon);
                    bool flag2 = miniEndlessGrid.meleePositionsCount > 0;
                    miniEndlessGrid.meleePositionsCount = Math.Max(0, miniEndlessGrid.meleePositionsCount - numberOfSecondUncommon);
                    if (flag || flag2)
                    {
                        if (miniEndlessGrid.uncommonAntiBuffer < 0f)
                        {
                            miniEndlessGrid.uncommonAntiBuffer = 0f;
                        }
                        if (flag)
                        {
                            miniEndlessGrid.uncommonAntiBuffer += (endlessGridPerfab.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Stalker || endlessGridPerfab.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                        }
                        if (flag2)
                        {
                            miniEndlessGrid.uncommonAntiBuffer += (endlessGridPerfab.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Stalker || endlessGridPerfab.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                        }
                        baseSpawnPoint -= (!(flag && flag2)) ? 1 : 2;
                    }
                }
            }
        }
        else
        {
            miniEndlessGrid.uncommonAntiBuffer--;
        }
        
        /*// 4. 预测 Special 敌人（需要 currentWave > 15）
        if (currentWave > 15 && miniEndlessGrid.specialAntiBuffer <= 0 && baseSpawnPoint > 0 && miniEndlessGrid.meleePositionsCount > 0)
        {
            // 模拟随机数范围：Random.Range(0, num2 + 1)
            int maxPossibleSpecial = num2 + 1;
            
            // 计算期望值
            float expectedSpecial = maxPossibleSpecial / 2f;
            
            // 考虑 buffer 的影响
            if (specialAntiBuffer <= -2)
            {
                expectedSpecial = Math.Max(expectedSpecial, 1f);
            }
            
            // 不能超过可用位置
            expectedSpecial = Math.Min(expectedSpecial, meleePositionsCount);
            
            // 这里还需要考虑点数是否足够（简化处理）
            // 假设每个 special 敌人平均消耗 30 点
            int maxByPoints = availablePoints / 30;
            expectedSpecial = Math.Min(expectedSpecial, maxByPoints);
            
            specialCount = Mathf.FloorToInt(expectedSpecial);
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
}