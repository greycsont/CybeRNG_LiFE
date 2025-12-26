using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Unity.AI.Navigation;

using CybeRNG_LiFE.RNG;
using CybeRNG_LiFE.Cheats;

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
 * Vanilla game behavior from top to buttom:
 * Start(random pattern order and shuffle once)
 * OnTriggerEnter(Add spawn points by previous waves and shuffle once)
 * NextWave(add previous points and load pattern, if the index of pattern pool exceed length then shuffle and reset index to 0)
 * Load pattern
 * OneDone(when all cubes in there target position, spawn hideous mass) *
 * GetEnemies(randomize spawn position and spawn uncommon and special enemies)
 * GetNextEnemy(spawn normal enemies)
 *
 * Current Idea:
 * For the wave started, the globalRNG has a fixed seed, then it will generate RNG for:
 * Pattern, Enemy Spawn Position, Enemy Behavior
 * But the code of enmey is messy, so only first of two are used,
 *
 * TODO list:
 * - [x] Remove randomization in Start()
 * - [x] Replace UnityEngine.Random.Range in related functions
 * - [x] fixed pattern in each run
 * - [Maybe] fixed enemy spawn catgeory (how:predetermine the Onedone,GetEnemies and GetNextEnemy calls?)
 */

[HarmonyPatch(typeof(EndlessGrid))]
public class EndlessGridPatch
{
    private static readonly MethodInfo IRNGRangeIntMI = AccessTools.Method(typeof(RandomManager), nameof(RandomManager.RangeInt), new[] { typeof(int), typeof(int), typeof(RNGScope)});
    private static readonly MethodInfo IRNGRangeFloatMI = AccessTools.Method(typeof(RandomManager), nameof(RandomManager.RangeFloat), new[] { typeof(float), typeof(float), typeof(RNGScope)});

    private static readonly MethodInfo UnityRangeIntMI = AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(int), typeof(int)});
    private static readonly MethodInfo UnityRangeFloatMI = AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(float), typeof(float)});

    private static void RandomizePatternFromEndlessGridStart(EndlessGrid endlessGrid)
    {
        for (int k = 0; k < endlessGrid.CurrentPatternPool.Length; k++)
		{
			ArenaPattern arenaPattern = endlessGrid.CurrentPatternPool[k];
			int num = RandomManager.patternRNG.Range(k, endlessGrid.CurrentPatternPool.Length);
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
        RandomManager.TryToInitializeRNG();
    }
    
    /// <summary>
    /// Here! here's what this transpiler is doing
    /// Add RandomizePatternFromEndlessGridStart()
    /// Insert RoundRNGAndPattern() in the for loop
    /// Move ShuffleDecks() from buttom to up
    /// The order of these manipulation is from top to down in the transpiler
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.OnTriggerEnter))]
    public static IEnumerable<CodeInstruction> OnTriggerEnterTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        // currentWave = startWave - 1;
		//IL_0023: ldarg.0
		//IL_0024: ldarg.0
		//IL_0025: ldfld int32 EndlessGrid::startWave
		//IL_002a: ldc.i4.1
		//IL_002b: sub
		//IL_002c: stfld int32 EndlessGrid::currentWave
        //------------------ insert RandomizePatternFromEndlessGridStart() here  ------------------
        //------------------ move ShuffleDecks() from bottom to here  ------------------
		// for (int i = 1; i <= currentWave; i++)
		//IL_0031: ldc.i4.1
		//IL_0032: stloc.0
		// (no C# code)
        //IL_0033: br.s IL_004b
		// loop start (head: IL_004b)
		//	// maxPoints += 3 + i / 3;
		//	IL_0035: ldarg.0
		//	IL_0036: ldarg.0
		//	IL_0037: ldfld int32 EndlessGrid::maxPoints
		//	IL_003c: ldc.i4.3
		//	IL_003d: ldloc.0
		//	IL_003e: ldc.i4.3
		//	IL_003f: div
		//	IL_0040: add
		//	IL_0041: add
		//	IL_0042: stfld int32 EndlessGrid::maxPoints
        //  ------------------ insert RoundRNGAndPattern() here ------------------
		//	// for (int i = 1; i <= currentWave; i++)
		//	IL_0047: ldloc.0
		//	IL_0048: ldc.i4.1
		//	IL_0049: add
		//	IL_004a: stloc.0
        //
		//	// for (int i = 1; i <= currentWave; i++)
		//	IL_004b: ldloc.0
		//	IL_004c: ldarg.0
		//	IL_004d: ldfld int32 EndlessGrid::currentWave
        //  IL_0052: ble.s IL_0035
		// end loop
        // SetGlowColor(roundDown: true);
		//IL_0054: ldarg.0
		//IL_0055: ldc.i4.1
		//IL_0056: call instance void EndlessGrid::SetGlowColor(bool)
		// waveNumberText.transform.parent.parent.gameObject.SetActive(value: true);
		//IL_005b: ldarg.0
		//IL_005c: ldfld class [UnityEngine.UI]UnityEngine.UI.Text EndlessGrid::waveNumberText
		//IL_0061: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Transform [UnityEngine.CoreModule]UnityEngine.Component::get_transform()
		//IL_0066: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Transform [UnityEngine.CoreModule]UnityEngine.Transform::get_parent()
		//IL_006b: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Transform [UnityEngine.CoreModule]UnityEngine.Transform::get_parent()
		//IL_0070: callvirt instance class [UnityEngine.CoreModule]UnityEngine.GameObject [UnityEngine.CoreModule]UnityEngine.Component::get_gameObject()
		//IL_0075: ldc.i4.1
		//IL_0076: callvirt instance void [UnityEngine.CoreModule]UnityEngine.GameObject::SetActive(bool)
		// ShuffleDecks();
        // ------------------ Remove these two lines of IL code ------------------
		//IL_007b: ldarg.0
		//IL_007c: call instance void EndlessGrid::ShuffleDecks()
        // -----------------------------------------------------------------------
		// NextWave();
		//IL_0081: ldarg.0
		//IL_0082: call instance void EndlessGrid::NextWave()
		// (no C# code)
		//IL_0087: ret
        
        var matcher = new CodeMatcher(instructions);

        matcher
        .MatchForward(
            false,
            new CodeMatch(
                OpCodes.Stfld, 
                AccessTools.Field(typeof(EndlessGrid),nameof(EndlessGrid.currentWave))
            )
        )
        .Advance(1)
        .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(typeof(EndlessGridPatch),nameof(EndlessGridPatch.RandomizePatternFromEndlessGridStart))
            ),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(typeof(EndlessGrid), nameof(EndlessGrid.ShuffleDecks))
            )
        )
        .MatchForward(
            false,
            new CodeMatch(
                OpCodes.Stfld,
                AccessTools.Field(typeof(EndlessGrid),nameof(EndlessGrid.maxPoints))
            )
        )
        .Advance(1)
        .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(typeof(PredetermineManager), nameof(PredetermineManager.RoundCurrentPattern))
            )
        )
        .MatchForward(
            false,
            new CodeMatch(
                OpCodes.Call,
                AccessTools.Method(typeof(EndlessGrid), nameof(EndlessGrid.ShuffleDecks))
            )
        )    
        .Advance(-1)
        .RemoveInstructions(2);

        return matcher.InstructionEnumeration();
    }


    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.NextWave))]
    public static void NextWave_Prefix() => RandomManager.FreshRNG();


    // --- Remove Start Shuffdeck etc. ---
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EndlessGrid.Start))]
    public static bool RuinStartShuffling(EndlessGrid __instance)
    {
        RandomManager.seeded = CheatsManager.KeepCheatsEnabled
                  && PrefsManager.Instance.GetBool($"cheat.{UsingCustomRNGCheat.IDENTIFIER}");

        if(RandomManager.seeded == false) return true;

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
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.ShuffleDecks))]
    public static IEnumerable<CodeInstruction> ShuffleDecks_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
            matcher
            .Set(OpCodes.Call, IRNGRangeIntMI)
            .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.Pattern));

        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher
            .Set(OpCodes.Call, IRNGRangeFloatMI)
            .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.Pattern));

        return matcher.InstructionEnumeration();
    }


    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.GetEnemies))]
    public static IEnumerable<CodeInstruction> GetEnemies_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        int callCount = 0;

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
        {
            if (callCount < 2)
            {
                matcher
                .Set(OpCodes.Call, IRNGRangeIntMI)
                .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.CubePosition));
                callCount++;
            }
            else
            {
                matcher
                .Set(OpCodes.Call, IRNGRangeIntMI)
                .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.EnemySpawn));
            }

        }


        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher
            .Set(OpCodes.Call, IRNGRangeFloatMI)
            .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.EnemySpawn));

        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(EndlessGrid.GetNextEnemy))]
    public static IEnumerable<CodeInstruction> GetNextEnemy_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Replacing UnityEngine.Random(int, int)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeIntMI)).IsValid)
            matcher
            .Set(OpCodes.Call, IRNGRangeIntMI)
            .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.EnemySpawn));

        // Reset cursor
        matcher.Start();

        // Replacing UnityEngine.Random(float, float)
        while (matcher.MatchForward(false, new CodeMatch(OpCodes.Call, UnityRangeFloatMI)).IsValid)
            matcher
            .Set(OpCodes.Call, IRNGRangeFloatMI)
            .Insert(RandomManager.GetCodeInstructionOfRNGScope(RNGScope.EnemySpawn));

        return matcher.InstructionEnumeration();
    }
}