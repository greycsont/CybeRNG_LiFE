using System;
using System.Collections.Generic;
using UnityEngine;
using CybeRNG_LiFE.RNG;

namespace CybeRNG_LiFE;

/// <summary>
/// A Mini CyberGrind Model used for store and calculating predetermine results.
/// </summary>
public struct MiniEndlessGrid
{
    public int currentWave = 0;
    public int massAntiBuffer;
    public float uncommonAntiBuffer;
    public int specialAntiBuffer;
    public int meleePositionsCount;
    public int projectilePositionsCount;
    public int hideousMassPositionCount;
    public int usedMeleePosition = 0;
    public int usedProjectilePosition = 0;
    public int hideousMassSpawned = 0;
    public int uncommonSpawned = 0;
    public int specialSpawned = 0;
    public List<EnemyTypeTracker> spawnedEnemyTypes = new List<EnemyTypeTracker>();
    public int baseSpawnPoint = 0;
    public int points;
    public MiniEndlessGrid(int massAntiBuffer, float uncommonAntiBuffer, int specialAntiBuffer, int points)
    {
        this.massAntiBuffer = massAntiBuffer;
        this.uncommonAntiBuffer = uncommonAntiBuffer;
        this.specialAntiBuffer = specialAntiBuffer;
        this.points = points;
    }
    public void ClearForNextWave()
    {
        meleePositionsCount = 0;
        projectilePositionsCount = 0;
        hideousMassPositionCount = 0;
        baseSpawnPoint = 0;
        hideousMassSpawned = 0;
        uncommonSpawned = 0;
        specialSpawned = 0;
        usedMeleePosition = 0;
        usedProjectilePosition = 0;
        spawnedEnemyTypes.Clear();
    }
    public void SetPositionCount(int meleePositionsCount, int projectilePositionsCount, int hideousMassPositionCount)
    {
        this.meleePositionsCount = meleePositionsCount;
        this.projectilePositionsCount = projectilePositionsCount;
        this.hideousMassPositionCount = hideousMassPositionCount;
    }

    public void PredetermineSpawn(EndlessGrid endlessGrid)
    {
        Plugin.Logger.LogDebug($"[PredetermineSpawn] Wave: {currentWave}");
        Plugin.Logger.LogDebug($"{hideousMassPositionCount}, {meleePositionsCount}, {projectilePositionsCount}");
        PredetermineHideous();
        PredetermineUncommonAndSpecial(endlessGrid);

        Plugin.Logger.LogDebug($"H: {hideousMassSpawned} U: {uncommonSpawned} S: {specialSpawned}");
        Plugin.Logger.LogDebug($"AH: {massAntiBuffer} AU: {uncommonAntiBuffer} AS: {specialAntiBuffer}");
        ClearForNextWave();
    }

    public int GetIndexOfEnemyType(EnemyType target)
    {
        if (spawnedEnemyTypes.Count > 0)
        {
            for (int i = 0; i < spawnedEnemyTypes.Count; i++)
            {
                if (spawnedEnemyTypes[i].type == target)
                {
                    return i;
                }
            }
        }
        spawnedEnemyTypes.Add(new EnemyTypeTracker(target));
        return spawnedEnemyTypes.Count - 1;
    }

    private void PredetermineHideous()
    {
        int hideousMassesCount = 0;

        for (int i = 0; i < hideousMassPositionCount; i++)
        {
            if (massAntiBuffer == 0 &&
                currentWave >= (hideousMassesCount + 1) * 10 &&
                points > 70)
            {
                hideousMassesCount++;
                points -= 45;
            }
        }
        hideousMassSpawned = hideousMassesCount;
        massAntiBuffer = Math.Max(0, massAntiBuffer += hideousMassSpawned > 0 ? hideousMassSpawned * 2 : -1);
    }

    public bool DetermineSpawnRadiant(EndlessEnemy target, int indexOfEnemyType)
    {
        float num = target.spawnWave * 2 + 25;
		float num2 = target.spawnCost;
		if (target.spawnCost < 10)
		{
			num2 += 1f;
		}
		if (target.spawnCost > 10)
		{
			num2 = num2 / 2f + 5f;
		}
		return (float)currentWave >= num + (float)spawnedEnemyTypes[indexOfEnemyType].amount * num2;
    }

    public bool DetermineUncommonSpawn(int target, int amount, EndlessGrid endlessGrid)
    {
        amount = endlessGrid.CapUncommonsAmount(target, amount);
        var spawnResult = false;
        for (int i = 0; i < amount; i++)
        {
            var endlessEnemy = endlessGrid.prefabs.uncommonEnemies[target];
            bool spawnOnProjectile = endlessEnemy.enemyType != EnemyType.Stalker && endlessEnemy.enemyType != EnemyType.Guttertank && RandomManager.enemySpawnRNG.Range(0f, 1f) > 0.5f;
            if (spawnOnProjectile && projectilePositionsCount <= 0)
            {
                spawnOnProjectile = false;
            }
            if (meleePositionsCount <= 0)
            {
                break;
            }
            // IDK why but the original did that
            var indexOfEnemyType = GetIndexOfEnemyType(endlessEnemy.enemyType);
            int extraPointReduced = endlessEnemy.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType].amount;
            bool isSpawnRadiantSuccessfully = DetermineSpawnRadiant(endlessEnemy, indexOfEnemyType);
            // I think it can be write as | isSpawnRadiantSuccessfully ? 3 : 1 |
            points -= endlessEnemy.spawnCost * ((!isSpawnRadiantSuccessfully) ? 1 : 3) + extraPointReduced;
            spawnedEnemyTypes[indexOfEnemyType].amount++;
            if (spawnOnProjectile)
            {
                projectilePositionsCount--;
            }
            else
            {
                meleePositionsCount--;
            }
            spawnResult = true;
            uncommonSpawned++;
            if (isSpawnRadiantSuccessfully)
            {
                amount -= 2;
            }
        }
        return spawnResult;
    }

    private void PredetermineUncommonAndSpecial(EndlessGrid endlessGrid)
    {
        baseSpawnPoint = currentWave / 10;
        baseSpawnPoint -= hideousMassSpawned;

        if (baseSpawnPoint <= 0) return;

        if (currentWave > 11)
        {
            if (uncommonAntiBuffer < 1f && baseSpawnPoint > 0)
            {
                // It need to consume a RNG value so don't use lambda expression here
                var numberOfFirstUncommon = RandomManager.enemySpawnRNG.Range(0, currentWave / 10 + 1);
                if (uncommonAntiBuffer <= -0.5f && numberOfFirstUncommon < 1)
                    numberOfFirstUncommon = 1;

                if (numberOfFirstUncommon > 0 && meleePositionsCount > 0)
                {
                    int firstUncommonIndex = RandomManager.enemySpawnRNG.Range(0, endlessGrid.prefabs.uncommonEnemies.Length);
                    int secondUncommonIndex = RandomManager.enemySpawnRNG.Range(0, endlessGrid.prefabs.uncommonEnemies.Length);
                    int numberOfSecondUncommon = 0;

                    while (firstUncommonIndex >= 0 && currentWave < endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].spawnWave)
                        firstUncommonIndex--;

                    while (secondUncommonIndex >= 0 && (currentWave < endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].spawnWave || secondUncommonIndex == firstUncommonIndex))
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

                        bool isFirstUncommonSpawnSuccessfully = DetermineUncommonSpawn(firstUncommonIndex, numberOfFirstUncommon, endlessGrid);
                        bool isSecondUncommonSpawnSuccessfully = false;
                        if (numberOfSecondUncommon > 0)
                            isSecondUncommonSpawnSuccessfully = DetermineUncommonSpawn(secondUncommonIndex, numberOfSecondUncommon, endlessGrid);                    
                        if (isFirstUncommonSpawnSuccessfully || isSecondUncommonSpawnSuccessfully)
                        {
                            if (uncommonAntiBuffer < 0f)
                            {
                                uncommonAntiBuffer = 0f;
                            }
                            if (isFirstUncommonSpawnSuccessfully)
                            {
                                uncommonAntiBuffer += (endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Stalker 
                                                                    || endlessGrid.prefabs.uncommonEnemies[firstUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                            }
                            if (isSecondUncommonSpawnSuccessfully)
                            {
                                uncommonAntiBuffer += (endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Stalker 
                                                                    || endlessGrid.prefabs.uncommonEnemies[secondUncommonIndex].enemyType == EnemyType.Idol) ? 1f : 0.5f;
                            }
                            baseSpawnPoint -= (!(isFirstUncommonSpawnSuccessfully && isSecondUncommonSpawnSuccessfully)) ? 1 : 2;
                        }
                    }
                }
            }
            else
            {
                uncommonAntiBuffer -= 1f;
            }
            if (currentWave > 15)
            {
                var isSpawnSpecialSuccessfully = false;
                if (specialAntiBuffer <= 0 && baseSpawnPoint > 0)
                {
                    int numberOfSpecial = RandomManager.enemySpawnRNG.Range(0, baseSpawnPoint + 1);
                    if (specialAntiBuffer <= -2 && numberOfSpecial < 1)
                    {
                        numberOfSpecial = 1;
                    }
                    if (numberOfSpecial > 0 && meleePositionsCount > 0)
                    {
                        for (int i = 0; i < numberOfSpecial; i++)
                        {
                            int indexOfSpecial = RandomManager.enemySpawnRNG.Range(0, endlessGrid.prefabs.specialEnemies.Length);
                            int indexOfEnemyType = GetIndexOfEnemyType(endlessGrid.prefabs.specialEnemies[indexOfSpecial].enemyType);
                            float extraPointReduced = 0f;
                            while (indexOfSpecial >= 0 && meleePositionsCount > 0)
                            {
                                if (currentWave >= endlessGrid.prefabs.specialEnemies[indexOfSpecial].spawnWave 
                                    && points >= endlessGrid.prefabs.specialEnemies[indexOfSpecial].spawnCost + extraPointReduced)
                                {
                                    bool isSpawnRadiantSuccessfully = DetermineSpawnRadiant(endlessGrid.prefabs.specialEnemies[indexOfSpecial], indexOfEnemyType);
                                    points -= Mathf.RoundToInt((float)(endlessGrid.prefabs.specialEnemies[indexOfSpecial].spawnCost * ((!isSpawnRadiantSuccessfully) ? 1 : 3)) + extraPointReduced);
                                    extraPointReduced += endlessGrid.prefabs.specialEnemies[indexOfSpecial].costIncreasePerSpawn * ((!isSpawnRadiantSuccessfully) ? 1 : 3);
                                    spawnedEnemyTypes[indexOfEnemyType].amount++;
                                    meleePositionsCount--;
                                    if (specialAntiBuffer < 0)
                                    {
                                        specialAntiBuffer = 0;
                                    }
                                    specialAntiBuffer++;
                                    specialSpawned++;
                                    isSpawnSpecialSuccessfully = true;
                                    break;
                                }
                                indexOfSpecial--;
                                if (indexOfSpecial >= 0)
                                {
                                    indexOfEnemyType = GetIndexOfEnemyType(endlessGrid.prefabs.specialEnemies[indexOfSpecial].enemyType);
                                }
                            }
                        }
                    }
                }
                if (!isSpawnSpecialSuccessfully)
                {
                    specialAntiBuffer--;
                }
            }
        }
    }
}