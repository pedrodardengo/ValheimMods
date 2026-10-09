using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VisualImpairmentSupport;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class VisualImpairmentSupportPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.visualimpairmentsupport.plugin";
    private const string PluginName = "Visual Impairment Support";
    private const string PluginVersion = "1.0.4";

    private void Awake()
    {
        new Harmony(PluginGuid).PatchAll();
        Logger.LogInfo("Installed inventory item slot color outlines.");
    }
}

[HarmonyPatch]
internal static class InventoryGridUpdateGuiPatch
{
    private const string BorderObjectName = "VisualImpairmentSupportBorder";
    private static readonly Color ItemBorderColor = new Color(1f, 0.12f, 0.12f, 1f);

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(InventoryGrid)))
        {
            if (method.Name == "UpdateGui" || method.Name == "UpdateInventory")
            {
                yield return method;
            }
        }
    }

    [HarmonyPostfix]
    private static void Postfix(InventoryGrid __instance)
    {
        List<InventoryElement> elements = Traverse.Create(__instance).Field("m_elements").GetValue<List<InventoryElement>>();
        if (elements == null)
        {
            return;
        }

        Inventory inventory = __instance.GetInventory();
        foreach (InventoryElement element in elements)
        {
            if (element == null)
            {
                continue;
            }

            ItemDrop.ItemData item = inventory == null
                ? null
                : inventory.GetItemAt(element.Position.x, element.Position.y);

            RectTransform slotRect = element.GetElementRectTransform();
            if (slotRect == null)
            {
                continue;
            }

            Transform borderTransform = slotRect.Find(BorderObjectName);
            SlotBorderGraphic border = borderTransform == null ? null : borderTransform.GetComponent<SlotBorderGraphic>();
            if (border == null)
            {
                GameObject borderObject = new GameObject(
                    BorderObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(SlotBorderGraphic)
                );
                RectTransform borderRect = borderObject.GetComponent<RectTransform>();
                borderRect.SetParent(slotRect, false);
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = Vector2.zero;
                borderRect.offsetMax = Vector2.zero;
                border = borderObject.GetComponent<SlotBorderGraphic>();
                border.raycastTarget = false;
            }

            border.transform.SetAsLastSibling();
            if (TryGetColor(item))
            {
                border.color = ItemBorderColor;
                border.enabled = true;
                border.SetVerticesDirty();
            }
            else
            {
                border.enabled = false;
            }
        }
    }

    private static bool TryGetColor(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
        {
            return false;
        }

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Tool:
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Hands:
            case ItemDrop.ItemData.ItemType.Shoulder:
            case ItemDrop.ItemData.ItemType.Shield:
            case ItemDrop.ItemData.ItemType.Utility:
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                return true;

            default:
                return false;
        }
    }
}

internal sealed class SlotBorderGraphic : MaskableGraphic
{
    private const float BorderThickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float thickness = Mathf.Min(BorderThickness, Mathf.Min(rect.width, rect.height) / 2f);
        UIVertex[] quad = new UIVertex[4];

        AddQuad(vertexHelper, quad, rect.xMin, rect.yMax - thickness, rect.xMax, rect.yMax);
        AddQuad(vertexHelper, quad, rect.xMin, rect.yMin, rect.xMax, rect.yMin + thickness);
        AddQuad(vertexHelper, quad, rect.xMin, rect.yMin + thickness, rect.xMin + thickness, rect.yMax - thickness);
        AddQuad(vertexHelper, quad, rect.xMax - thickness, rect.yMin + thickness, rect.xMax, rect.yMax - thickness);
    }

    private void AddQuad(VertexHelper vertexHelper, UIVertex[] quad, float xMin, float yMin, float xMax, float yMax)
    {
        Color32 vertexColor = color;
        quad[0] = CreateVertex(xMin, yMin, vertexColor);
        quad[1] = CreateVertex(xMin, yMax, vertexColor);
        quad[2] = CreateVertex(xMax, yMax, vertexColor);
        quad[3] = CreateVertex(xMax, yMin, vertexColor);
        vertexHelper.AddUIVertexQuad(quad);
    }

    private static UIVertex CreateVertex(float x, float y, Color32 vertexColor)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = new Vector3(x, y, 0f);
        vertex.color = vertexColor;
        return vertex;
    }
}