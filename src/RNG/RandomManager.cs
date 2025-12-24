using System;
using System.Collections.Generic;

namespace CybeRNG_LiFE.RNG;

public class RandomManager
{
    public static int seed = 114514;
    internal static bool seeded = false;
    
    // patternRNG is independent with waveRNG
    public static IRandomNumberGenerator patternRNG;
    public static IRandomNumberGenerator waveRNG;
    public static IRandomNumberGenerator enemySpawnRNG;
    public static IRandomNumberGenerator enemyBehaviorRNG;

    private static RNGScope CurrentScope => ScopeStack.Count > 0 ? ScopeStack.Peek() : RNGScope.Default;
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
}