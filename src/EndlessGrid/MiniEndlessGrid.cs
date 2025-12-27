using System.Collections.Generic;
using CybeRNG_LiFE.RNG;

namespace CybeRNG_LiFE;

public struct MiniEndlessGrid
{
    public int currentWave = 0;
    public int massAntiBuffer;
    public float uncommonAntiBuffer;
    public int specialAntiBuffer;
    public int meleePositionsCount;
    public int projectilePositionsCount;
    public int hideousMassPositionCount;
    public int numberOfHideousMass = 0;
    public List<EnemyTypeTracker> spawnedEnemyTypes;
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
        massAntiBuffer = 0;
        uncommonAntiBuffer = 0;
        specialAntiBuffer = 0;
        meleePositionsCount = 0;
        projectilePositionsCount = 0;
        hideousMassPositionCount = 0;
        baseSpawnPoint = 0;
        numberOfHideousMass = 0;
        spawnedEnemyTypes.Clear();
    }
    public void SetPositionCount(int meleePositionsCount, int projectilePositionsCount, int hideousMassPositionCount)
    {
        this.meleePositionsCount = meleePositionsCount;
        this.projectilePositionsCount = projectilePositionsCount;
        this.hideousMassPositionCount = hideousMassPositionCount;
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
            int extraPointReduced = endlessEnemy.costIncreasePerSpawn * spawnedEnemyTypes[target].amount;
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
            if (isSpawnRadiantSuccessfully)
            {
                amount -= 2;
            }
        }
        return spawnResult;
    }
}