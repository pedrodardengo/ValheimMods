using System;
using System.Text;

namespace ItemAnnouncer
{
    internal static class Announcer
    {
        private const int DuplicateWindowMs = 400;

        private static string _lastText;
        private static int _lastTick;

        internal static void OnMiddleClick(InventoryGrid grid, ItemDrop.ItemData item)
        {
            try
            {
                if (Plugin.Ativar == null || !Plugin.Ativar.Value)
                {
                    return;
                }

                if (item == null || item.m_shared == null)
                {
                    return;
                }

                string text = BuildText(item);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                Speak(text);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogError("ItemAnnouncer erro ao anunciar: " + ex);
                }
            }
        }

        internal static void OnRecipeMiddleClick(Recipe recipe, ItemDrop.ItemData item)
        {
            try
            {
                if (Plugin.Ativar == null || !Plugin.Ativar.Value || recipe == null)
                {
                    return;
                }

                string text = BuildRecipeText(recipe, item);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Speak(text);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogError("ItemAnnouncer erro ao anunciar receita: " + ex);
                }
            }
        }

        private static string BuildText(ItemDrop.ItemData item)
        {
            string name = GetItemName(item);
            return Format(Plugin.Formato.Value, name, item.m_stack);
        }

        private static string BuildRecipeText(Recipe recipe, ItemDrop.ItemData item)
        {
            ItemDrop.ItemData product = item ?? (recipe.m_item == null ? null : recipe.m_item.m_itemData);
            if (product == null || product.m_shared == null)
            {
                return null;
            }

            string name = GetItemName(product);
            StringBuilder text = new StringBuilder(name);
            Piece.Requirement[] requirements = recipe.m_resources;
            if (requirements == null || requirements.Length == 0)
            {
                return text.ToString();
            }

            text.Append(": ");
            bool hasRequirement = false;
            int quality = Math.Max(1, product.m_quality);
            CraftingStation station = Player.m_localPlayer == null
                ? null
                : Player.m_localPlayer.GetCurrentCraftingStation();
            bool isUpgrader = station != null && station.m_upgrader;
            foreach (Piece.Requirement requirement in requirements)
            {
                if (requirement == null || requirement.m_resItem == null || requirement.m_resItem.m_itemData == null)
                {
                    continue;
                }

                if (requirement.m_upgraderResource != isUpgrader)
                {
                    continue;
                }

                int amount = requirement.GetAmount(quality);
                if (amount <= 0)
                {
                    continue;
                }

                if (hasRequirement)
                {
                    text.Append(", ");
                }

                text.Append(GetItemName(requirement.m_resItem.m_itemData));
                text.Append(' ');
                text.Append(amount);
                hasRequirement = true;
            }

            return text.ToString();
        }

        private static void Speak(string text)
        {
            int now = Environment.TickCount;
            if (text == _lastText && unchecked(now - _lastTick) < DuplicateWindowMs)
            {
                return;
            }

            _lastText = text;
            _lastTick = now;
            TtsClient.Speak(text);
        }

        private static string Format(string template, string name, int count)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                template = "{name}: {count}";
            }

            return template
                .Replace("{name}", name)
                .Replace("{count}", count.ToString());
        }

        private static string GetItemName(ItemDrop.ItemData item)
        {
            string raw = item.m_shared.m_name;
            string name = raw;

            try
            {
                Localization localization = Localization.instance;
                if (localization != null)
                {
                    name = localization.Localize(raw);
                }
            }
            catch
            {
                name = raw;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = raw;
            }

            if (!string.IsNullOrEmpty(name) && name[0] == '$')
            {
                name = name.Substring(1);
                int index = name.IndexOf('_');
                if (index >= 0 && name.StartsWith("item_", StringComparison.Ordinal))
                {
                    name = name.Substring(index + 1);
                }
                name = name.Replace('_', ' ');
            }

            return name;
        }
    }
}
