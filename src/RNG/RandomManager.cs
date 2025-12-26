using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CybeRNG_LiFE.RNG;

public class RandomManager
{
    public static int seed = 114514;
    internal static bool seeded = false;
    
    // patternRNG is independent with waveRNG
    public static IRandomNumberGenerator patternRNG;
    public static IRandomNumberGenerator waveRNG;
    public static IRandomNumberGenerator cubePositionRNG;
    public static IRandomNumberGenerator enemySpawnRNG;
    public static IRandomNumberGenerator enemyBehaviorRNG;

    public static void TryToInitializeRNG()
    {
        if(seeded == false) return;

        waveRNG = new PCG32(seed);

        patternRNG = new PCG32(seed, 2);

        FreshRNG();
    }
    public static void FreshRNG()
    {
        Plugin.Logger.LogInfo("Freshing RNG for EndlessGrid");
        GenerateRNG((int)waveRNG.NextUInt());
    }

    public static void GenerateRNG(int seed)
    {
        cubePositionRNG = new Xoshiro128StarStar(seed);
        enemySpawnRNG = new Xoshiro128StarStar(seed);
    }

    public static int RangeInt(int min, int max, RNGScope scope)
    {
        if(seeded == false) return UnityEngine.Random.Range(min, max);
        if(scope == RNGScope.Default) ShowFuckUpSubtitle();
        return scope switch
        {
            RNGScope.Pattern       => patternRNG.Range(min, max),
            RNGScope.CubePosition  => cubePositionRNG.Range(min, max),
            RNGScope.EnemySpawn    => enemySpawnRNG.Range(min, max),
            RNGScope.EnemyBehavior => enemyBehaviorRNG.Range(min, max),
            _ => UnityEngine.Random.Range(min, max)
        };
    }

    public static float RangeFloat(float min, float max, RNGScope scope)
    {
        if(seeded == false) return UnityEngine.Random.Range(min, max);
        if(scope == RNGScope.Default) ShowFuckUpSubtitle();
        return scope switch
        {
            RNGScope.Pattern       => patternRNG.Range(min, max),
            RNGScope.CubePosition  => cubePositionRNG.Range(min, max),
            RNGScope.EnemySpawn    => enemySpawnRNG.Range(min, max),
            RNGScope.EnemyBehavior => enemyBehaviorRNG.Range(min, max),
            _ => UnityEngine.Random.Range(min, max)
        };
    }

    private static void ShowFuckUpSubtitle() => 
        SubtitleController.Instance.DisplaySubtitle("If you are reading this it means this mod is fucked up. Fuck infinite-state Machine", ignoreSetting: false);

    // IDK where to put it so i put it here
    public static CodeInstruction GetCodeInstructionOfRNGScope(RNGScope scope)
    {
        return new CodeInstruction(OpCodes.Ldc_I4, (int)scope);
    }
}