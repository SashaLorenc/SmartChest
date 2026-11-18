using System.Collections.Generic;
using SmartChest.Catalog;
using SmartChest.Chests;
using SmartChest.Compat;
using SmartChest.UI;
using UnityEngine;

namespace SmartChest.Storing
{
    /// <summary>Recherche d'un objet par son nom dans les coffres proches : les coffres trouvés brillent et affichent la quantité.</summary>
    public static class ChestSearch
    {
        private class Hit
        {
            public string Text;
            public float Until;
        }

        private static readonly Dictionary<Container, Hit> _hits = new Dictionary<Container, Hit>();

        /// <summary>Texte à afficher au-dessus du coffre s'il fait partie des résultats d'une recherche en cours.</summary>
        public static string GetHitText(Container c)
        {
            if (c == null || !_hits.TryGetValue(c, out Hit hit)) return null;
            if (Time.time > hit.Until)
            {
                _hits.Remove(c);
                return null;
            }
            return hit.Text;
        }

        public static void Run(Player player, string query)
        {
            if (player == null) return;
            query = (query ?? "").Trim();
            if (query.Length == 0) return;
            string q = query.ToLowerInvariant();

            _hits.Clear();
            float duration = Plugin.SearchDuration.Value;
            int found = 0;

            foreach (Container c in ContainerRegistry.All())
            {
                if (Vector3.Distance(player.transform.position, c.transform.position) > Plugin.SearchRadius.Value) continue;
                if (!ModCompat.IsEligibleChest(c)) continue;
                Inventory inv = c.GetInventory();
                if (inv == null) continue;

                int count = 0;
                string name = null;
                foreach (ItemDrop.ItemData item in inv.m_inventory)
                {
                    string local = Localization.instance.Localize(item.m_shared.m_name);
                    string prefab = CatalogManager.GetPrefabName(item);
                    if (local.ToLowerInvariant().Contains(q) || prefab.ToLowerInvariant().Contains(q))
                    {
                        count += item.m_stack;
                        if (name == null) name = local;
                    }
                }
                if (count == 0) continue;

                found++;
                _hits[c] = new Hit { Text = $"{name} x{count}", Until = Time.time + duration };
                ChestHighlight.Flash(c, duration);
            }

            string msg = found == 0
                ? string.Format(Localization.instance.Localize("$smartchest_search_none"), query)
                : string.Format(Localization.instance.Localize("$smartchest_search_found"), query, found);
            player.Message(MessageHud.MessageType.Center, msg);
        }
    }
}
