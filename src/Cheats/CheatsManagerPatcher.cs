using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CybeRNG_LiFE.Cheats;

[HarmonyPatch(typeof(CheatsManager))]
public static class CheatsManagerPatcher
{
    [HarmonyTranspiler]
    [HarmonyPatch("Start")]
    private static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        // IL_000c: ldstr "meta"
        // IL_0011: call instance void CheatsManager::RegisterCheat(class ICheat, string)

        matcher.Start()
               .MatchForward(useEnd: true,
                    new CodeMatch(OpCodes.Ldstr, "meta"),
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CheatsManager), nameof(CheatsManager.RegisterCheat)))
               )
              .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.Call(static () => RegisterCheats(null!)));
        return matcher.InstructionEnumeration();
    }
    private static void RegisterCheats(CheatsManager __instance)
    {
        if (SceneHelper.CurrentScene != "Endless") return;
        Plugin.Logger.LogInfo("Registering SeedOverrideCheat");
        __instance.RegisterCheat(new UsingCustomRNGCheat(), "meta");
    }
}