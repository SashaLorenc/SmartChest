using System;
using System.Collections.Generic;
using System.Linq;
using SmartChest.Catalog;

namespace SmartChest.Storing
{
    /// <summary>
    /// Tri du contenu d'un coffre : par catégorie (dans l'ordre du catalogue), puis par nom.
    /// Même méthode que Quick Stack Store : on fusionne les piles incomplètes, puis on réattribue les cases.
    /// </summary>
    public static class SortService
    {
        public static void SortChest(Player player, Container container)
        {
            if (player == null || container == null || container.m_nview == null || !container.m_nview.IsValid()) return;

            // Le jeu donne la propriété du coffre à celui qui l'ouvre : sinon un autre joueur l'utilise
            if (!container.m_nview.IsOwner())
            {
                player.Message(MessageHud.MessageType.Center, "$smartchest_msg_not_owner");
                return;
            }

            Inventory inv = container.GetInventory();
            if (inv == null) return;

            var items = new List<ItemDrop.ItemData>(inv.m_inventory);
            MergeStacks(items, inv);

            // Rang (position de la première catégorie dans le catalogue) et nom traduit, calculés une fois par objet
            var rank = new Dictionary<ItemDrop.ItemData, int>();
            var names = new Dictionary<ItemDrop.ItemData, string>();
            foreach (ItemDrop.ItemData item in items)
            {
                rank[item] = CatalogManager.SortRank(item);
                names[item] = Localization.instance.Localize(item.m_shared.m_name);
            }

            items.Sort((a, b) =>
            {
                int c = rank[a].CompareTo(rank[b]);
                if (c != 0) return c;
                c = string.Compare(names[a], names[b], StringComparison.CurrentCultureIgnoreCase);
                if (c != 0) return c;
                c = b.m_quality.CompareTo(a.m_quality);
                if (c != 0) return c;
                return b.m_stack.CompareTo(a.m_stack);
            });

            int index = 0;
            for (int y = 0; y < inv.GetHeight() && index < items.Count; y++)
                for (int x = 0; x < inv.GetWidth() && index < items.Count; x++)
                    items[index++].m_gridPos = new Vector2i(x, y);

            inv.Changed();
            player.Message(MessageHud.MessageType.Center, "$smartchest_msg_sorted");
        }

        /// <summary>Regroupe les piles incomplètes d'un même objet (copié de Quick Stack Store).</summary>
        private static void MergeStacks(List<ItemDrop.ItemData> items, Inventory inventory)
        {
            var groups = items
                .Where(i => i.m_stack < i.m_shared.m_maxStackSize && (i.m_customData == null || i.m_customData.Count == 0))
                .GroupBy(i => i.m_shared.m_name + "#" + i.m_quality)
                .Select(g => g.ToList())
                .ToList();

            foreach (List<ItemDrop.ItemData> stacks in groups)
            {
                if (stacks.Count <= 1) continue;
                int remaining = stacks.Sum(i => i.m_stack);
                int max = stacks[0].m_shared.m_maxStackSize;
                foreach (ItemDrop.ItemData item in stacks)
                {
                    if (remaining <= 0)
                    {
                        item.m_stack = 0;
                        inventory.RemoveItem(item);
                        items.Remove(item);
                    }
                    else
                    {
                        item.m_stack = Math.Min(max, remaining);
                        remaining -= item.m_stack;
                    }
                }
            }
        }
    }
}
