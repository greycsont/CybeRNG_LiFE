using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Unity.AI.Navigation;

using CybeRNG_LiFE.RNG;
using CybeRNG_LiFE.Cheats;
using System.Net;

namespace CybeRNG_LiFE;

// 首先，这个神秘的pattern是在start里随机排列的
// 需要用自定义随机数的方法有：
// Start最开始的抽卡循环，或者先复制然后再游戏开始时替换start里的值
// ShuffleDecks()
// GetEnemies()
// GetNexeEnemies()

// 他的pattern设计的也是一大坨，先在start里随机排列然后洗牌，走完后再归0重新洗
// 问题是你一开始洗两次牌干嘛

/*
 * Current Idea:
 * For the wave started, the globalRNG has a fixed seed, then it will generate RNG for:
 * Pattern, Enemy Spawn Position, Enemy Behavior
 * But the code of enmey is messy, so only first of two are used,
 *
 * TODO list:
 * - [x] Remove randomization in Start()
 * - [x] Replace UnityEngine.Random.Range in related functions
 * - [x] fixed pattern in each run
 * - [Maybe] fixed enemy spawn catgeory
 */

[HarmonyPatch(typeof(EndlessGrid))]
public class EndlessGridPatch
{
    public static int seed = 114514;
    private static bool seeded = false;
    
    // patternRNG is independent with waveRNG
    public static IRandomNumberGenerator patternRNG;
    public static IRandomNumberGenerator waveRNG;
    public static IRandomNumberGenerator enemySpawnRNG;
    public static IRandomNumberGenerator enemyBehaviorRNG;
    public static void FreshRNG()
    {
        Plugin.Logger.LogInfo("Freshing RNG for EndlessGrid");
        GenerateRNG((int)waveRNG.NextUInt());
    }

    [ThreadStatic]
    private static Stack<RNGScope> _scopeStack;

    private static Stack<RNGScope> ScopeStack
    {
        get
        {
            if (_scopeStack == null)
                _scopeStack = new Stack<RNGScope>();
            return _scopeStack;
        }
    }

    private static RNGScope CurrentScope => ScopeStack.Count > 0 ? ScopeStack.Peek() : RNGScope.Default;

    public static void PushScope(RNGScope scope)
    {
        ScopeStack.Push(scope);
    }

    public static void PopScope()
    {
        if (ScopeStack.Count > 0)
            ScopeStack.Pop();
    }

    public static void GenerateRNG(int seed)
    {
        enemySpawnRNG = new Xoshiro128StarStar(seed);
    }

    public static int RangeInt(int min, int max)
    {
        if(seeded == false) return UnityEngine.Random.Range(min, max);
        if(CurrentScope == RNGScope.Default) ShowFuckUpSubtitle();
        return CurrentScope switch
        {
            RNGScope.Pattern       => patternRNG.Range(min, max),
            RNGScope.EnemySpawn    => enemySpawnRNG.Range(min, max),
            RNGScope.EnemyBehavior => enemyBehaviorRNG.Range(min, max),
            _ => UnityEngine.Random.Range(min, max)
        };
    }

    public static float RangeFloat(float min, float max)
    {
        if(seeded == false) return UnityEngine.Random.Range(min, max);
        if(CurrentScope == RNGScope.Default) ShowFuckUpSubtitle();
        return CurrentScope switch
        {
            RNGScope.Pattern       => patternRNG.Range(min, max),
            RNGScope.EnemySpawn    => enemySpawnRNG.Range(min, max),
            RNGScope.EnemyBehavior => enemyBehaviorRNG.Range(min, max),
            _ => UnityEngine.Random.Range(min, max)
        };
    }

    private static void ShowFuckUpSubtitle() => 
        SubtitleController.Instance.DisplaySubtitle("If you are reading this it means this mod is fucked up. Fuck infinite-state Machine", ignoreSetting: false);

    private static readonly MethodInfo IRNGRangeIntMI = AccessTools.Method(typeof(EndlessGridPatch), nameof(RangeInt));
    private static readonly MethodInfo IRNGRangeFloatMI = AccessTools.Method(typeof(EndlessGridPatch), nameof(RangeFloat));

    private static readonly MethodInfo UnityRangeIntMI = AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(int), typeof(int) });
    private static readonly MethodInfo UnityRangeFloatMI = AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(float), typeof(float) });

    private static void RoundCurrentPatternByWave(EndlessGrid endlessGrid)
    {
        for(int i = 0; i < endlessGrid.startWave - 1; i++)
        {
            FreshRNG();
            endlessGrid.currentPatternNum++;
            if(endlessGrid.currentPatternNum >= endlessGrid.CurrentPatternPool.Length)
                endlessGrid.ShuffleDecks();
        }
    }

    private static void RandomizePatternAtEndlessGridStart(EndlessGrid endlessGrid)
    {
        for (int k = 0; k < endlessGrid.CurrentPatternPool.Length; k++)
		{
			ArenaPattern arenaPattern = endlessGrid.CurrentPatternPool[k];
			int num = patternRNG.Range(k, endlessGrid.CurrentPatternPool.Length);
			endlessGrid.CurrentPatternPool[k] = endlessGrid.CurrentPatternPool[num];
			endlessGrid.CurrentPatternPool[num] = arenaPattern;
		}

        endlessGrid.ShuffleDecks();
    }
    // ======= Harmony Patch =======
    // --- RNG Generate Related ---
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.OnTriggerEnter))]
    public static void TryToGenerateRandomizer(ref Collider other)
    {
        if(!other.CompareTag("Player")) return;

        if(seeded == false) return;

        waveRNG = new PCG32(seed);

        patternRNG = new PCG32(seed, 2);

        FreshRNG();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.OnTriggerEnter))]
    public static IEnumerable<CodeInstruction> InsertFreshRNGAfterShuffleDeck(
        IEnumerable<CodeInstruction> instructions)
    {
        // ------------------ insert RandomizePatternAtEndlessGridStart() here  ------------------
        // ShuffleDecks();
		//IL_007b: ldarg.0   <- unity did this shit so I did it too
		//IL_007c: call instance void EndlessGrid::ShuffleDecks()
        // ------------------ insert RoundCurrentPatternByWave() here ------------------
		// NextWave();
		//IL_0081: ldarg.0
		//IL_0082: call instance void EndlessGrid::NextWave()

        var matcher = new CodeMatcher(instructions);

        if (!matcher.MatchForward(
                false,
                new CodeMatch(
                    OpCodes.Call, 
                    AccessTools.Method(typeof(EndlessGrid),nameof(EndlessGrid.ShuffleDecks))
                )
            ).IsValid)
        {
            Plugin.Logger.LogError(
                "Failed to find call EndlessGrid::ShuffleDecks() in OnTriggerEnter"
            );
            return instructions;
        }

        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(typeof(EndlessGridPatch),nameof(RandomizePatternAtEndlessGridStart)
                )
            )
        );

        matcher.Advance(1);

        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(typeof(EndlessGridPatch),nameof(RoundCurrentPatternByWave)
                )
            )
        );

        return matcher.InstructionEnumeration();
    }


    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.NextWave))]
    public static void NextWave_Prefix() => FreshRNG();


    // --- Remove Start Shuffdeck etc. ---
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.Start))]
    public static bool RuinStartShuffling(EndlessGrid __instance)
    {
        seeded = CheatsManager.KeepCheatsEnabled
                  && PrefsManager.Instance.GetBool($"cheat.{UsingCustomRNGCheat.IDENTIFIER}");

        if(seeded == false) return true;

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

        Plugin.Logger.LogInfo($"Removed ShuffleDeck and randomization on Endlessgrid::Start().");

        return false;
    }

    // --- UnityEngine.Random.Range Replacement ---
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.ShuffleDecks))]
    public static void ShuffleDeck_Prefix() => PushScope(RNGScope.Pattern);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EndlessGrid.ShuffleDecks))]
    public static void ShuffleDeck_Postfix() => PopScope();

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.ShuffleDecks))]
    public static IEnumerable<CodeInstruction> ShuffleDecks_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeIntMI);

        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeFloatMI);

        return matcher.InstructionEnumeration();
    }


    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.GetEnemies))]
    public static void GetEnemies_Prefix() => PushScope(RNGScope.EnemySpawn);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EndlessGrid.GetEnemies))]
    public static void GetEnemies_Postfix() => PopScope();

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.GetEnemies))]
    public static IEnumerable<CodeInstruction> GetEnemies_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeIntMI);

        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeFloatMI);

        return matcher.InstructionEnumeration();
    }


    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.GetNextEnemy))]
    public static void GetNextEnemies_Prefix() => PushScope(RNGScope.EnemySpawn);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EndlessGrid.GetNextEnemy))]
    public static void GetNextEnemies_Postfix() => PopScope();

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.GetNextEnemy))]
    public static IEnumerable<CodeInstruction> GetNextEnemy_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeIntMI);

        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher.Set(OpCodes.Call, IRNGRangeFloatMI);

        return matcher.InstructionEnumeration();
    }


}