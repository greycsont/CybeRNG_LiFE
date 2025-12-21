using GameConsole;
using HarmonyLib;

namespace CybeRNG_LiFE.Commands;

[HarmonyPatch(typeof(Console))]
public class ConsolePatcher
{
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    public static void AddConsoleCommands(Console __instance)
    {
        var Command = new CommandsToRegister(__instance);
        __instance.RegisterCommand(Command);
    }
}