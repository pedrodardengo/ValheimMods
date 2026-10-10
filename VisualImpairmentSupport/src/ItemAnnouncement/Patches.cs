using HarmonyLib;
using System.Collections;
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
            if (!Input.GetMouseButtonDown(2))
            {
                return;
            }

            InventoryGrid playerGrid = Traverse.Create(__instance).Field("m_playerGrid").GetValue<InventoryGrid>();
            if (TryAnnounceHoveredItem(playerGrid))
            {
                return;
            }

            InventoryGrid containerGrid = Traverse.Create(__instance).Field("m_containerGrid").GetValue<InventoryGrid>();
            if (TryAnnounceHoveredItem(containerGrid))
            {
                return;
            }

            TryAnnounceHoveredRecipe(__instance);
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

        private static void TryAnnounceHoveredRecipe(InventoryGui gui)
        {
            IEnumerable recipes = Traverse.Create(gui).Field("m_availableRecipes").GetValue<IEnumerable>();
            if (recipes == null)
            {
                return;
            }

            foreach (object recipeEntry in recipes)
            {
                if (recipeEntry == null)
                {
                    continue;
                }

                GameObject element = Traverse.Create(recipeEntry).Property("InterfaceElement").GetValue<GameObject>();
                RectTransform rect = element == null ? null : element.transform as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy ||
                    !RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition))
                {
                    continue;
                }

                Recipe recipe = Traverse.Create(recipeEntry).Property("Recipe").GetValue<Recipe>();
                ItemDrop.ItemData item = Traverse.Create(recipeEntry).Property("ItemData").GetValue<ItemDrop.ItemData>();
                Announcer.OnRecipeMiddleClick(recipe, item);
                return;
            }
        }
    }
}
