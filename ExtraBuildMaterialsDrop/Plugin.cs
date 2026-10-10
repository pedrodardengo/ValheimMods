using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ExtraBuildMaterialsDrop;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class ExtraBuildMaterialsDropPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.extrabuildmaterialsdrop.plugin";
    private const string PluginName = "Extra Build Materials Drop";
    private const string PluginVersion = "1.1.0";

    internal static ConfigEntry<float> MaterialDropMultiplier;

    private void Awake()
    {
        MaterialDropMultiplier = Config.Bind(
            "General",
            "Material drop multiplier",
            2f,
            new ConfigDescription(
                "Overrides the world's resource rate for these materials from trees and rocks only: Wood, Fine Wood, Core Wood (RoundLog), Ancient Bark, Yggdrasil Wood, Ashwood, Blackwood, Stone, Black Marble, and Grausten. Other sources, such as chests and creatures, are unaffected.",
                new AcceptableValueList<float>(1f, 1.5f, 2f, 2.5f, 3f)));

        new Harmony(PluginGuid).PatchAll();
        Logger.LogInfo($"Tree and rock wood and stone drops use a {MaterialDropMultiplier.Value}x multiplier.");
    }
}

[HarmonyPatch(typeof(TreeBase), "Awake")]
internal static class TreeBaseAwakePatch
{
    [HarmonyPostfix]
    private static void Postfix(TreeBase __instance)
    {
        ResourceMaterialDropTables.Register(
            Traverse.Create(__instance).Field("m_dropWhenDestroyed").GetValue<DropTable>());
    }
}

[HarmonyPatch(typeof(TreeLog), "Awake")]
internal static class TreeLogAwakePatch
{
    [HarmonyPostfix]
    private static void Postfix(TreeLog __instance)
    {
        ResourceMaterialDropTables.Register(
            Traverse.Create(__instance).Field("m_dropWhenDestroyed").GetValue<DropTable>());
    }
}

[HarmonyPatch(typeof(MineRock), "Start")]
internal static class MineRockStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(MineRock __instance)
    {
        ResourceMaterialDropTables.Register(
            Traverse.Create(__instance).Field("m_dropItems").GetValue<DropTable>());
    }
}

[HarmonyPatch(typeof(MineRock5), "Awake")]
internal static class MineRock5AwakePatch
{
    [HarmonyPostfix]
    private static void Postfix(MineRock5 __instance)
    {
        ResourceMaterialDropTables.Register(
            Traverse.Create(__instance).Field("m_dropItems").GetValue<DropTable>());
    }
}

[HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new Type[] { typeof(int) })]
internal static class DropTableGetDropListPatch
{
    [HarmonyPostfix]
    private static void Postfix(DropTable __instance, List<GameObject> __result)
    {
        float multiplier = ExtraBuildMaterialsDropPlugin.MaterialDropMultiplier.Value;
        if (multiplier == 1f || __result == null || !ResourceMaterialDropTables.Contains(__instance))
        {
            return;
        }

        Dictionary<GameObject, int> materialCounts = new Dictionary<GameObject, int>();
        foreach (GameObject prefab in __result)
        {
            if (!ResourceMaterialDropTables.IsMaterial(prefab))
            {
                continue;
            }

            if (materialCounts.TryGetValue(prefab, out int count))
            {
                materialCounts[prefab] = count + 1;
            }
            else
            {
                materialCounts.Add(prefab, 1);
            }
        }

        foreach (KeyValuePair<GameObject, int> material in materialCounts)
        {
            int targetCount = Mathf.RoundToInt(material.Value * multiplier);
            for (int copyIndex = material.Value; copyIndex < targetCount; copyIndex++)
            {
                __result.Add(material.Key);
            }
        }
    }
}

internal static class ResourceMaterialDropTables
{
    private static readonly HashSet<string> BuildMaterials = new HashSet<string>(StringComparer.Ordinal)
    {
        "Wood",
        "FineWood",
        "RoundLog",
        "AncientBark",
        "YggdrasilWood",
        "Ashwood",
        "Blackwood",
        "Stone",
        "BlackMarble",
        "Grausten"
    };

    private static readonly HashSet<DropTable> RegisteredTables = new HashSet<DropTable>();

    internal static void Register(DropTable table)
    {
        if (table == null || !RegisteredTables.Add(table))
        {
            return;
        }

        IEnumerable drops = Traverse.Create(table).Field("m_drops").GetValue<IEnumerable>();
        if (drops == null)
        {
            return;
        }

        foreach (object drop in drops)
        {
            Traverse dropData = Traverse.Create(drop);
            GameObject prefab = dropData.Field("m_item").GetValue<GameObject>();
            if (prefab != null && BuildMaterials.Contains(prefab.name))
            {
                dropData.Field("m_dontScale").SetValue(true);
            }
        }
    }

    internal static bool Contains(DropTable table)
    {
        return RegisteredTables.Contains(table);
    }

    internal static bool IsMaterial(GameObject prefab)
    {
        return prefab != null && BuildMaterials.Contains(prefab.name);
    }
}