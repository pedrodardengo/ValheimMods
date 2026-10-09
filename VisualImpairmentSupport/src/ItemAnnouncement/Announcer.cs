using System;

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

                string text = BuildText(grid, item);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                int now = Environment.TickCount;
                if (text == _lastText && unchecked(now - _lastTick) < DuplicateWindowMs)
                {
                    return;
                }
                _lastText = text;
                _lastTick = now;

                TtsClient.Speak(text);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogError("ItemAnnouncer erro ao anunciar: " + ex);
                }
            }
        }

        private static string BuildText(InventoryGrid grid, ItemDrop.ItemData item)
        {
            string name = GetItemName(item);
            bool stackable = item.m_shared.m_maxStackSize > 1;

            if (!stackable)
            {
                return Format(Plugin.Formato.Value, name, 1, 1);
            }

            if (Plugin.AnunciarTotal != null && Plugin.AnunciarTotal.Value)
            {
                int total = GetTotal(grid, item);
                return Format(Plugin.FormatoComTotal.Value, name, item.m_stack, total);
            }

            return Format(Plugin.Formato.Value, name, item.m_stack, item.m_stack);
        }

        private static string Format(string template, string name, int count, int total)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                template = "{name}: {count}";
            }

            return template
                .Replace("{name}", name)
                .Replace("{count}", count.ToString())
                .Replace("{total}", total.ToString());
        }

        private static int GetTotal(InventoryGrid grid, ItemDrop.ItemData item)
        {
            try
            {
                Inventory inventory = grid != null ? grid.GetInventory() : null;
                if (inventory == null)
                {
                    return item.m_stack;
                }

                int total = 0;
                foreach (ItemDrop.ItemData other in inventory.GetAllItems())
                {
                    if (other != null && other.m_shared != null && other.m_shared.m_name == item.m_shared.m_name)
                    {
                        total += other.m_stack;
                    }
                }
                return total;
            }
            catch
            {
                return item.m_stack;
            }
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
