using BepInEx;
using HarmonyLib;

namespace VisualImpairmentSupport;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class VisualImpairmentSupportPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.visualimpairmentsupport.plugin";
    private const string PluginName = "Visual Impairment Support";
    private const string PluginVersion = "1.1.2";
    private Harmony _harmony;

    private void Awake()
    {
        ItemAnnouncer.Plugin.Initialize(this, Logger);
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo("Installed inventory item slot color outlines and item announcements.");
    }

    private void OnDestroy()
    {
        ItemAnnouncer.Plugin.Shutdown();
        _harmony?.UnpatchSelf();
    }
}