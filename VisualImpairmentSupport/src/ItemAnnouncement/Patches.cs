using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ItemAnnouncer
{
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class MiddleClickItemPatch
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryGui __instance)
        {
            if (Plugin.FalarCliqueMeio == null || !Plugin.FalarCliqueMeio.Value || !Input.GetMouseButtonDown(2))
            {
                return;
            }

            InventoryGrid playerGrid = Traverse.Create(__instance).Field("m_playerGrid").GetValue<InventoryGrid>();
            if (TryAnnounceHoveredItem(playerGrid))
            {
                return;
            }

            InventoryGrid containerGrid = Traverse.Create(__instance).Field("m_containerGrid").GetValue<InventoryGrid>();
            TryAnnounceHoveredItem(containerGrid);
        }

        private static bool TryAnnounceHoveredItem(InventoryGrid grid)
        {
            if (grid == null)
            {
                return false;
            }

            List<InventoryElement> elements = Traverse.Create(grid).Field("m_elements").GetValue<List<InventoryElement>>();
            if (elements == null)
            {
                return false;
            }

            foreach (InventoryElement element in elements)
            {
                if (element == null)
                {
                    continue;
                }

                RectTransform slotRect = element.GetElementRectTransform();
                if (slotRect == null || !slotRect.gameObject.activeInHierarchy ||
                    !RectTransformUtility.RectangleContainsScreenPoint(slotRect, Input.mousePosition))
                {
                    continue;
                }

                Inventory inventory = grid.GetInventory();
                ItemDrop.ItemData item = inventory == null
                    ? null
                    : inventory.GetItemAt(element.Position.x, element.Position.y);
                Announcer.OnMiddleClick(grid, item);
                return true;
            }

            return false;
        }
    }
}
