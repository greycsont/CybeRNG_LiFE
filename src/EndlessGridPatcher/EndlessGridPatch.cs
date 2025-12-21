using HarmonyLib;
using UnityEngine;
using Unity.AI.Navigation;

using CybeRNG_LiFE.RNG;

namespace CybeRNG_LiFE;

/*
 * Current Idea:
 * For the wave started, the globalRNG has a fixed seed, then it will generate RNG for:
 * Pattern, Enemy Spawn Position, Enemy Behavior
 * But the code of enmey is messy, so only first of two are used,
 *
 *
 *
 *
 *
 */

[HarmonyPatch(typeof(EndlessGrid))]
public class EndlessGridPatch
{
    public static int seed = 114514;
    public static IRandomNumberGenerator globalRNG;
    public static IRandomNumberGenerator patternRNG;
    public static IRandomNumberGenerator enemySpawnRNG;
    public static IRandomNumberGenerator enemyBehaviorRNG;
    public static bool seedRun = true;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.Start))]
    public static bool RuinStartShuffling(EndlessGrid __instance)
    {
        if (seedRun == false) return true;
        //seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

        var eg = __instance;

        eg.nms = eg.GetComponent<NavMeshSurface>();
		eg.anw = eg.GetComponent<ActivateNextWave>();
		eg.gz = GoreZone.ResolveGoreZone(eg.transform);
		eg.cubes = new EndlessCube[16][];
		for (int i = 0; i < 16; i++)
		{
			eg.cubes[i] = new EndlessCube[16];
			for (int j = 0; j < 16; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(eg.gridCube, eg.transform, worldPositionStays: true);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3((float)i * eg.offset, 0f, (float)j * eg.offset);
				eg.cubes[i][j] = gameObject.GetComponent<EndlessCube>();
				eg.cubes[i][j].positionOnGrid = new Vector2Int(i, j);
			}
		}
		/*for (int k = 0; k < __instance.CurrentPatternPool.Length; k++)
		{
			ArenaPattern arenaPattern = __instance.CurrentPatternPool[k];
			int num = UnityEngine.Random.Range(k, __instance.CurrentPatternPool.Length);
			__instance.CurrentPatternPool[k] = __instance.CurrentPatternPool[num];
			__instance.CurrentPatternPool[num] = arenaPattern;
		}*/
		eg.crorea = MonoSingleton<CrowdReactions>.Instance;
		if (eg.crorea != null)
		{
			eg.crowdReactions = true;
		}

		//__instance.ShuffleDecks();

		PresenceController.UpdateCyberGrindWave(0);
		eg.mats = eg.GetComponentInChildren<MeshRenderer>().sharedMaterials;
		Material[] array = eg.mats;
		foreach (Material obj in array)
		{
			obj.SetColor(UKShaderProperties.EmissiveColor, Color.blue);
			obj.SetFloat(UKShaderProperties.EmissiveIntensity, 0.2f * eg.glowMultiplier);
			obj.SetFloat("_PCGamerMode", 0f);
			obj.SetFloat("_GradientScale", 2f);
			obj.SetFloat("_GradientFalloff", 5f);
			obj.SetFloat("_GradientSpeed", 10f);
			obj.SetVector("_WorldOffset", new Vector4(0f, 0f, 62.5f, 0f));
			eg.targetColor = Color.blue;
		}
		eg.TrySetupStaticGridMesh();
		int? highestWaveForDifficulty = WaveUtils.GetHighestWaveForDifficulty(MonoSingleton<PrefsManager>.Instance.GetInt("difficulty"));
		int num2 = MonoSingleton<PrefsManager>.Instance.GetInt("cyberGrind.startingWave");
		eg.startWave = (WaveUtils.IsWaveSelectable(num2, highestWaveForDifficulty.GetValueOrDefault()) ? num2 : 0);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.ShuffleDecks))]
    public static bool ShuffleDeckWithCustomRNG(EndlessGrid __instance)
    {
        if (seedRun == false) return true;

        var eg = __instance;

        int num = Mathf.FloorToInt(eg.CurrentPatternPool.Length / 2);
		for (int i = 0; i < num; i++)
		{
			ArenaPattern arenaPattern = eg.CurrentPatternPool[i];
			int num2 = globalRNG.Range(i, num);
			eg.CurrentPatternPool[i] = eg.CurrentPatternPool[num2];
			eg.CurrentPatternPool[num2] = arenaPattern;
		}
		for (int j = num; j < eg.CurrentPatternPool.Length; j++)
		{
			ArenaPattern arenaPattern2 = eg.CurrentPatternPool[j];
			int num3 = globalRNG.Range(j, eg.CurrentPatternPool.Length);
			eg.CurrentPatternPool[j] = eg.CurrentPatternPool[num3];
			eg.CurrentPatternPool[num3] = arenaPattern2;
		}

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.OnTriggerEnter))]
    public static void TryToGenerateRandomizer(ref Collider other)
    {
        if(!other.CompareTag("Player")) return;
        globalRNG = new XorShift32(seed);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.NextWave))]
    public static void FreshRNG()
    {
        GenerateRNG(globalRNG.Range(0,2^32));
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.GetEnemies))]
    public static bool GetEnemiesWithCustomRNG(EndlessGrid __instance)
    {
        if (seedRun == false) return true;

        var eg = __instance;

        eg.nms.BuildNavMesh();
		eg.nvmhlpr.GenerateLinks(eg.cubes);
		for (int i = 0; i < eg.meleePositions.Count; i++)
		{
			Vector2 value = eg.meleePositions[i];
			int index = globalRNG.Range(i, eg.meleePositions.Count);
			eg.meleePositions[i] = eg.meleePositions[index];
			eg.meleePositions[index] = value;
		}
		for (int j = 0; j < eg.projectilePositions.Count; j++)
		{
			Vector2 value2 = eg.projectilePositions[j];
			int index2 = globalRNG.Range(j, eg.projectilePositions.Count);
			eg.projectilePositions[j] = eg.projectilePositions[index2];
			eg.projectilePositions[index2] = value2;
		}
		eg.tempEnemyAmount = 0;
		eg.usedMeleePositions = 0;
		eg.usedProjectilePositions = 0;
		eg.spawnedEnemyTypes.Clear();
		eg.tempEnemyAmount += eg.hideousMasses;
		eg.hideousMasses = 0;
		if (eg.currentWave > 11)
		{
			int num = eg.currentWave;
			int num2 = 0;
			while (num >= 10)
			{
				num -= 10;
				num2++;
			}
			if (eg.tempEnemyAmount > 0)
			{
				num2 -= eg.tempEnemyAmount;
			}
			if (eg.uncommonAntiBuffer < 1f && num2 > 0)
			{
				int num3 = globalRNG.Range(0, eg.currentWave / 10 + 1);
				if (eg.uncommonAntiBuffer <= -0.5f && num3 < 1)
				{
					num3 = 1;
				}
				if (num3 > 0 && eg.meleePositions.Count > 0)
				{
					int num4 = globalRNG.Range(0, eg.prefabs.uncommonEnemies.Length);
					int num5 = globalRNG.Range(0, eg.prefabs.uncommonEnemies.Length);
					int num6 = 0;
					while (num4 >= 0 && eg.currentWave < eg.prefabs.uncommonEnemies[num4].spawnWave)
					{
						num4--;
					}
					while (num5 >= 0 && (eg.currentWave < eg.prefabs.uncommonEnemies[num5].spawnWave || num5 == num4))
					{
						if (num5 == 0)
						{
							num6 = -1;
							break;
						}
						num5--;
					}
					if (num4 >= 0)
					{
						if (eg.currentWave > 16)
						{
							if (eg.currentWave < 25)
							{
								num3++;
							}
							else if (num6 != -1)
							{
								num6 = num3;
							}
						}
						bool flag = false;
						bool flag2 = false;
						flag = eg.SpawnUncommons(num4, num3);
						if (num6 > 0)
						{
							flag2 = eg.SpawnUncommons(num5, num6);
						}
						if (flag || flag2)
						{
							if (eg.uncommonAntiBuffer < 0f)
							{
								eg.uncommonAntiBuffer = 0f;
							}
							if (flag)
							{
								eg.uncommonAntiBuffer += ((eg.prefabs.uncommonEnemies[num4].enemyType == EnemyType.Stalker || eg.prefabs.uncommonEnemies[num4].enemyType == EnemyType.Idol) ? 1f : 0.5f);
							}
							if (flag2)
							{
								eg.uncommonAntiBuffer += ((eg.prefabs.uncommonEnemies[num5].enemyType == EnemyType.Stalker || eg.prefabs.uncommonEnemies[num5].enemyType == EnemyType.Idol) ? 1f : 0.5f);
							}
							num2 -= ((!(flag && flag2)) ? 1 : 2);
						}
					}
				}
			}
			else
			{
				eg.uncommonAntiBuffer -= 1f;
			}
			if (eg.currentWave > 15)
			{
				bool flag3 = false;
				if (eg.specialAntiBuffer <= 0 && num2 > 0)
				{
					int num7 = globalRNG.Range(0, num2 + 1);
					if (eg.specialAntiBuffer <= -2 && num7 < 1)
					{
						num7 = 1;
					}
					if (num7 > 0 && eg.meleePositions.Count > 0)
					{
						for (int k = 0; k < num7; k++)
						{
							int num8 = globalRNG.Range(0, eg.prefabs.specialEnemies.Length);
							int indexOfEnemyType = eg.GetIndexOfEnemyType(eg.prefabs.specialEnemies[num8].enemyType);
							float num9 = 0f;
							while (num8 >= 0 && eg.usedMeleePositions < eg.meleePositions.Count - 1)
							{
								if (eg.currentWave >= eg.prefabs.specialEnemies[num8].spawnWave && (float)eg.points >= (float)eg.prefabs.specialEnemies[num8].spawnCost + num9)
								{
									bool flag4 = eg.SpawnRadiant(eg.prefabs.specialEnemies[num8], indexOfEnemyType);
									eg.SpawnOnGrid(eg.prefabs.specialEnemies[num8].prefab, eg.meleePositions[eg.usedMeleePositions], prefab: false, enemy: true, CyberPooledType.None, flag4);
									eg.points -= Mathf.RoundToInt((float)(eg.prefabs.specialEnemies[num8].spawnCost * ((!flag4) ? 1 : 3)) + num9);
									num9 += (float)(eg.prefabs.specialEnemies[num8].costIncreasePerSpawn * ((!flag4) ? 1 : 3));
									eg.spawnedEnemyTypes[indexOfEnemyType].amount++;
									eg.usedMeleePositions++;
									eg.tempEnemyAmount++;
									if (eg.specialAntiBuffer < 0)
									{
										eg.specialAntiBuffer = 0;
									}
									eg.specialAntiBuffer++;
									flag3 = true;
									break;
								}
								num8--;
								if (num8 >= 0)
								{
									indexOfEnemyType = eg.GetIndexOfEnemyType(eg.prefabs.specialEnemies[num8].enemyType);
								}
							}
						}
					}
				}
				if (!flag3)
				{
					eg.specialAntiBuffer--;
				}
			}
		}
		eg.GetNextEnemy();

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.GetNextEnemy))]
    public static bool GetNextEnemyWithCustomRNG(EndlessGrid __instance)
    {
        if (seedRun == false) return true;

        var eg = __instance;

        if (!eg.gameObject.scene.isLoaded)
		{
			return false;
		}
		if ((eg.points > 0 && eg.usedMeleePositions < eg.meleePositions.Count) || (eg.points > 1 && eg.usedProjectilePositions < eg.projectilePositions.Count))
		{
			if ((globalRNG.Range(0f, 1f) < 0.5f || eg.usedProjectilePositions >= eg.projectilePositions.Count) && eg.usedMeleePositions < eg.meleePositions.Count)
			{
				int num = globalRNG.Range(0, eg.prefabs.meleeEnemies.Length);
				bool flag = false;
				for (int num2 = num; num2 >= 0; num2--)
				{
					EndlessEnemy endlessEnemy = eg.prefabs.meleeEnemies[num2];
					int indexOfEnemyType = eg.GetIndexOfEnemyType(endlessEnemy.enemyType);
					int num3 = endlessEnemy.costIncreasePerSpawn * eg.spawnedEnemyTypes[indexOfEnemyType].amount;
					int num4 = endlessEnemy.spawnCost + num3;
					if (((float)eg.points >= (float)num4 * 1.5f || (num2 == 0 && eg.points >= num4)) && eg.currentWave >= endlessEnemy.spawnWave)
					{
						bool flag2 = eg.SpawnRadiant(endlessEnemy, indexOfEnemyType);
						flag = true;
						eg.SpawnOnGrid(endlessEnemy.prefab, eg.meleePositions[eg.usedMeleePositions], prefab: false, enemy: true, CyberPooledType.None, flag2);
						eg.points -= endlessEnemy.spawnCost * ((!flag2) ? 1 : 3) + num3;
						eg.spawnedEnemyTypes[indexOfEnemyType].amount++;
						eg.usedMeleePositions++;
						eg.tempEnemyAmount++;
						break;
					}
				}
				if (!flag)
				{
					eg.usedMeleePositions =eg. meleePositions.Count;
				}
			}
			else if (eg.usedProjectilePositions < eg.projectilePositions.Count)
			{
				int num5 = globalRNG.Range(0, eg.prefabs.projectileEnemies.Length);
				bool flag3 = false;
				for (int num6 = num5; num6 >= 0; num6--)
				{
					EndlessEnemy endlessEnemy2 = eg.prefabs.projectileEnemies[num6];
					int indexOfEnemyType2 = eg.GetIndexOfEnemyType(endlessEnemy2.enemyType);
					int num7 = endlessEnemy2.costIncreasePerSpawn * eg.spawnedEnemyTypes[indexOfEnemyType2].amount;
					int num8 = endlessEnemy2.spawnCost + num7;
					if (((float)eg.points >= (float)num8 * 1.5f || (num6 == 0 && eg.points >= num8)) && eg.currentWave >= endlessEnemy2.spawnWave)
					{
						bool flag4 = eg.SpawnRadiant(endlessEnemy2, indexOfEnemyType2);
						flag3 = true;
						eg.SpawnOnGrid(endlessEnemy2.prefab, eg.projectilePositions[eg.usedProjectilePositions], prefab: false, enemy: true, CyberPooledType.None, flag4);
						eg.points -= endlessEnemy2.spawnCost * ((!flag4) ? 1 : 3) + num7;
						eg.spawnedEnemyTypes[indexOfEnemyType2].amount++;
						eg.usedProjectilePositions++;
						eg.tempEnemyAmount++;
						break;
					}
				}
				if (!flag3)
				{
					eg.usedProjectilePositions = eg.projectilePositions.Count;
				}
			}
			eg.Invoke("GetNextEnemy", 0.1f);
		}
		else
		{
			eg.enemyAmount = eg.tempEnemyAmount;
		}

        return false;
    }

    public static void GenerateRNG(int seed)
    {
        patternRNG = new PCG32(seed);
        enemySpawnRNG = new Xoshiro128StarStar(seed);
    }
}