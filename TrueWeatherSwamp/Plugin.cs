using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TrueWeatherSwamp;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class TrueWeatherSwampPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.trueweatherswamp.plugin";
    private const string PluginName = "True Weather Swamp";
    private const string PluginVersion = "1.1.4";

    internal static ManualLogSource ModLogger;

    private void Awake()
    {
        ModLogger = Logger;
        new Harmony(PluginGuid).PatchAll();
        Logger.LogInfo("Installed explicit Swamp weather pool.");
    }
}

internal static class SwampWeatherPool
{
    private static bool hasLoggedAppliedPool;
    private static bool hasLoggedMissingPatterns;

    internal static void Apply(EnvMan envMan, List<EnvEntry> availableEnvironments)
    {
        List<EnvEntry> swampEnvironments = new List<EnvEntry>(2);
        List<string> missingPatterns = new List<string>();

        EnvSetup clearSetup = FindEnvironment(envMan.m_environments, "Clear");
        clearSetup ??= FindEnvironment(envMan.m_environments, "Heath_clear");

        if (clearSetup == null)
        {
            missingPatterns.Add("Clear (or Heath_clear)");
            LogMissingPatterns(envMan, missingPatterns);
            return;
        }

        swampEnvironments.Add(new EnvEntry
        {
            m_environment = clearSetup.m_name,
            m_weight = 67f,
            m_env = clearSetup
        });

        EnvSetup swampRain = FindEnvironment(envMan.m_environments, "SwampRain");
        if (swampRain == null)
        {
            missingPatterns.Add("SwampRain");
        }
        else
        {
            swampEnvironments.Add(new EnvEntry
            {
                m_environment = swampRain.m_name,
                m_weight = 33f,
                m_env = swampRain
            });
        }

        LogMissingPatterns(envMan, missingPatterns);
        availableEnvironments.Clear();
        availableEnvironments.AddRange(swampEnvironments);
        if (!hasLoggedAppliedPool)
        {
            List<string> appliedPatterns = new List<string>(swampEnvironments.Count);
            foreach (EnvEntry entry in swampEnvironments)
            {
                appliedPatterns.Add($"{entry.m_environment} ({entry.m_weight})");
            }

            TrueWeatherSwampPlugin.ModLogger.LogInfo($"Applied Swamp weather pool: {string.Join(", ", appliedPatterns)}.");
            hasLoggedAppliedPool = true;
        }
    }

    private static EnvSetup FindEnvironment(List<EnvSetup> environments, string name)
    {
        foreach (EnvSetup environment in environments)
        {
            if (environment.m_name == name)
            {
                return environment;
            }
        }

        return null;
    }

    private static void LogMissingPatterns(EnvMan envMan, List<string> missingPatterns)
    {
        if (hasLoggedMissingPatterns || missingPatterns.Count == 0)
        {
            return;
        }

        List<string> availableNames = new List<string>(envMan.m_environments.Count);
        foreach (EnvSetup environment in envMan.m_environments)
        {
            availableNames.Add(environment.m_name);
        }

        TrueWeatherSwampPlugin.ModLogger.LogWarning(
            $"Weather patterns not found: {string.Join(", ", missingPatterns)}. Loaded environments: {string.Join(", ", availableNames)}.");
        hasLoggedMissingPatterns = true;
    }

}

[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetAvailableEnvironments))]
internal static class GetAvailableEnvironmentsPatch
{
    [HarmonyPostfix]
    private static void Postfix(EnvMan __instance, BiomeSector biome, List<EnvEntry> __result)
    {
        if (biome.Biome != Heightmap.Biome.Swamp || __result == null)
        {
            return;
        }

        SwampWeatherPool.Apply(__instance, __result);
    }
}
