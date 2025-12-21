using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CybeRNG_LiFE;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        var _harmony = new Harmony(PluginInfo.PLUGIN_GUID + ".harmony");
        _harmony.PatchAll();
    }
}
