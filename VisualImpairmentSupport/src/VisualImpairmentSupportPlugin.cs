using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace VisualImpairmentSupport;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class VisualImpairmentSupportPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.visualimpairmentsupport.plugin";
    private const string PluginName = "Visual Impairment Support";
    private const string PluginVersion = "1.2.1";
    private Harmony _harmony;
    internal static ConfigEntry<int> HighlightRed;
    internal static ConfigEntry<int> HighlightGreen;
    internal static ConfigEntry<int> HighlightBlue;
    private ConfigEntry<int> _highlightPreview;

    private void Awake()
    {
        var channelRange = new AcceptableValueRange<int>(0, 255);
        HighlightRed = Config.Bind("Important Item Highlight", "Red", 255,
            new ConfigDescription("Red channel of the outline color (0-255).", channelRange));
        HighlightGreen = Config.Bind("Important Item Highlight", "Green", 31,
            new ConfigDescription("Green channel of the outline color (0-255).", channelRange));
        HighlightBlue = Config.Bind("Important Item Highlight", "Blue", 31,
            new ConfigDescription("Blue channel of the outline color (0-255).", channelRange));
        _highlightPreview = Config.Bind("Important Item Highlight", "Color preview", 0,
            new ConfigDescription("Live preview of the current outline color.", null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = DrawHighlightPreview,
                    HideDefaultButton = true,
                    Order = -1
                }));
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

    private static void DrawHighlightPreview(ConfigEntryBase setting)
    {
        Rect previewRect = GUILayoutUtility.GetRect(72f, 24f, GUILayout.ExpandWidth(false));
        GUI.Box(previewRect, GUIContent.none);

        Rect colorRect = new Rect(
            previewRect.x + 2f,
            previewRect.y + 2f,
            previewRect.width - 4f,
            previewRect.height - 4f);
        Color previousColor = GUI.color;
        GUI.color = new Color32(
            (byte)HighlightRed.Value,
            (byte)HighlightGreen.Value,
            (byte)HighlightBlue.Value,
            255);
        GUI.DrawTexture(colorRect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}

internal sealed class ConfigurationManagerAttributes
{
    public Action<ConfigEntryBase> CustomDrawer;
    public bool? HideDefaultButton;
    public int? Order;
}