using System;
using System.Text.RegularExpressions;
using System.Linq;
using CybeRNG_LiFE.RNG;


namespace CybeRNG_LiFE;

public static class PredetermineManager
{
    // Greatest Shitpost ever
    public static int currentWave = 0;


    /// <summary>
    /// Round to the next pattern and predetermine spawns
    /// Automatically Rounding pattern with related RNG
    /// PreCalculate the Hideous mass, Uncommon and Special enmeys and cout them into the AntiBuffers
    /// </summary>
    /// <param name="endlessGrid"></param>
    public static void RoundCurrentPattern(EndlessGrid endlessGrid)
    {
        endlessGrid.currentPatternNum++;
        RandomManager.FreshRNG();
        if (endlessGrid.currentPatternNum >= endlessGrid.CurrentPatternPool.Length)
            endlessGrid.ShuffleDecks();

        currentWave++;

        //PredetermineSpawn(endlessGrid, currentWave);
    }
    /*
    private static void PredetermineSpawn(EndlessGrid endlessGrid, int currentWave)
    {
        var (Mcount, Pcount, Hcount) = ParsingPattern(endlessGrid.CurrentPatternPool[endlessGrid.currentPatternNum]);
        var hideousMassCount = PredetermineHideous(Hcount,
                                                   endlessGrid.massAntiBuffer,
                                                   endlessGrid.points,
                                                   currentWave);
        endlessGrid.massAntiBuffer += hideousMassCount;
        
        var (uncommonCount, specialCount) = PredetermineUncommonAndSpecial();
        endlessGrid.uncommonAntiBuffer += uncommonCount;
        endlessGrid.specialAntiBuffer += specialCount;
    }

    private static int PredetermineHideous(int Hcount, 
                                           int massAntiBuffer, 
                                           int points, 
                                           int currentWave)
    {
        int hideousMassesCount = 0;

        for (int i = 0; i < Hcount; i++)
        {
            if (massAntiBuffer == 0 && 
                currentWave >= (hideousMassesCount + 1) * 10 && 
                points > 70)
            {
                hideousMassesCount++;
                points -= 45;
                if (points <= 70) break;
            }
        }
        
        return hideousMassesCount > 0 ? hideousMassesCount * 2 : -1;
    }

    private static (float uncommonCount, int specialCount) PredetermineUncommonAndSpecial(int meleePositionsCount,
                                                                                          int projectilePositionsCount,
                                                                                          int HideousMassCount,
                                                                                          int currentWave,
                                                                                          float uncommonAntiBuffer,
                                                                                          int specialAntiBuffer)
    {
        float uncommonCount = 0f;
        int specialCount = 0;
        
        // 1. 计算 num2（基础数量）
        int num2 = currentWave / 10; // 等价于 while 循环
        int tempEnemyAmount = HideousMassCount;
        
        // 2. 减去已占用的位置（如果tempEnemyAmount代表已生成敌人）
        if (tempEnemyAmount > 0)
        {
            num2 = Math.Max(0, num2 - tempEnemyAmount);
        }
        
        // 如果没有可用数量，直接返回
        if (num2 <= 0) return (0f, 0);
        
        // 3. 预测 Uncommon 敌人
        if (uncommonAntiBuffer < 1f && meleePositionsCount > 0)
        {
            // 模拟随机数范围：Random.Range(0, currentWave / 10 + 1)
            int maxPossible = currentWave / 10 + 1;
            
            // 这是不确定的，但我们可以计算期望值
            float expectedUncommon = maxPossible / 2f; // 随机范围的中间值
            
            // 考虑 buffer 的影响
            if (uncommonAntiBuffer <= -0.5f)
            {
                expectedUncommon = Math.Max(expectedUncommon, 1f); // 至少1个
            }
            
            // 但不能超过可用的 num2
            expectedUncommon = Math.Min(expectedUncommon, num2);
            
            // 不能超过可用位置
            expectedUncommon = Math.Min(expectedUncommon, meleePositionsCount);
            
            uncommonCount = expectedUncommon;
            
            // 如果生成了 uncommon，需要减少可用数量
            if (uncommonCount > 0)
            {
                num2 -= Mathf.CeilToInt(uncommonCount); // 向上取整
            }
        }
        else
        {
            // uncommonAntiBuffer >= 1f，不生成 uncommon
            // 或者没有 melee 位置
        }
        
        // 4. 预测 Special 敌人（需要 currentWave > 15）
        if (currentWave > 15 && specialAntiBuffer <= 0 && num2 > 0 && meleePositionsCount > 0)
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
        }
        
        return (uncommonCount, specialCount);
    }

    private static (int Mcount, int Pcount, int Hcount) ParsingPattern(ArenaPattern currentPattern)
    {
        if (currentPattern == null || string.IsNullOrEmpty(currentPattern.prefabs)) return (0, 0, 0);

        ReadOnlySpan<char> prefabsSpan = currentPattern.prefabs.AsSpan();
        
        int Mcount = 0, Pcount = 0, Hcount = 0;
        int lineCount = 0;
        int lineLength = 0;
        
        for (int i = 0; i < prefabsSpan.Length && lineCount < 16; i++)
        {
            char c = prefabsSpan[i];
            
            if (c == '\n')
            {
                if (lineLength != 16) return (0, 0, 0);
                lineCount++;
                lineLength = 0;
                continue;
            }
            
            lineLength++;
            
            switch (c)
            {
                case 'n': Mcount++; break;
                case 'p': Pcount++; break;
                case 'H': Hcount++; break;
            }
        }
        
        return lineCount == 16 ? (Mcount, Pcount, Hcount) : (0, 0, 0);
    }*/
}