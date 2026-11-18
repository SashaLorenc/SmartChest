using System;
using System.Collections.Generic;
using SmartChest.Catalog;
using SmartChest.Chests;
using SmartChest.Compat;
using UnityEngine;

namespace SmartChest.Storing
{
    /// <summary>Recherche d'un objet par son nom (traduit ou nom interne) dans les coffres proches.</summary>
    public static class ChestSearch
    {
        public class Hit
        {
            public Container Container;
            /// <summary>Nom traduit du premier objet trouvé dans ce coffre.</summary>
            public string ItemName;
            public int Count;
        }

        /// <summary>Coffres à portée qui contiennent un objet dont le nom contient le texte cherché (sans tenir compte des majuscules).</summary>
        public static List<Hit> Find(Vector3 position, float radius, string query)
        {
            var hits = new List<Hit>();
            query = (query ?? "").Trim();
            if (query.Length == 0) return hits;

            foreach (Container c in ContainerRegistry.All())
            {
                if (Vector3.Distance(position, c.transform.position) > radius) continue;
                if (!ModCompat.IsEligibleChest(c)) continue;
                Inventory inv = c.GetInventory();
                if (inv == null) continue;

                Hit hit = null;
                foreach (ItemDrop.ItemData item in inv.m_inventory)
                {
                    string local = Localization.instance.Localize(item.m_shared.m_name);
                    if (!Contains(local, query) && !Contains(CatalogManager.GetPrefabName(item), query)) continue;
                    if (hit == null) hit = new Hit { Container = c, ItemName = local };
                    hit.Count += item.m_stack;
                }
                if (hit != null) hits.Add(hit);
            }
            return hits;
        }

        private static bool Contains(string text, string query) =>
            text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
