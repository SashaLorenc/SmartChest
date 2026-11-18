using System.Collections.Generic;
using System.Linq;
using SmartChest.Catalog;
using SmartChest.Chests;
using SmartChest.Compat;
using UnityEngine;

namespace SmartChest.Storing
{
    /// <summary>Le rangement par catégorie : déplace les objets de l'inventaire du joueur vers les bons coffres.</summary>
    public static class StoreService
    {
        private class Target
        {
            public Container Container;
            public Inventory Inventory;
            public HashSet<string> Categories;
            public ChestMode Mode;
            public float Distance;
            /// <summary>Coffre modifié à distance (pas celui que le joueur a ouvert).</summary>
            public bool Remote;
        }

        /// <summary>Range dans tous les coffres catégorisés à portée. Renvoie le nombre d'objets déplacés.</summary>
        public static void StoreNearby(Player player)
        {
            if (player == null) return;
            Container open = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;

            var targets = new List<Target>();
            foreach (Container c in ContainerRegistry.All())
            {
                float dist = Vector3.Distance(player.transform.position, c.transform.position);
                if (dist > Plugin.StoreRadius.Value) continue;
                Target t = MakeTarget(c, player, dist, c == open);
                if (t != null) targets.Add(t);
            }

            Run(player, targets);
        }

        /// <summary>Range uniquement dans le coffre ouvert, en ne déposant que les objets de ses catégories.</summary>
        public static void StoreInto(Player player, Container container)
        {
            if (player == null || container == null) return;
            Target t = MakeTarget(container, player, 0f, true);
            if (t == null)
            {
                player.Message(MessageHud.MessageType.Center, "$smartchest_msg_no_categories");
                return;
            }
            Run(player, new List<Target> { t });
        }

        private static Target MakeTarget(Container c, Player player, float distance, bool isOpenByMe)
        {
            // Vrai coffre posé par un joueur (pas un sac à dos, une tombe, un coffre de donjon...)
            if (!ModCompat.IsEligibleChest(c)) return null;
            Inventory inv = c.GetInventory();
            if (inv == null || inv == player.GetInventory()) return null;

            List<string> cats = ChestCategories.Get(c);
            if (cats.Count == 0) return null;

            if (!isOpenByMe)
            {
                // Coffre ouvert par un autre joueur, coffre privé, protégé par une balise, bateau en multi... : on n'y touche pas
                if (!ModCompat.CanAffectRemotely(c, player)) return null;
            }

            return new Target
            {
                Container = c,
                Inventory = inv,
                Categories = new HashSet<string>(cats),
                Mode = ChestCategories.GetMode(c),
                Distance = distance,
                Remote = !isOpenByMe,
            };
        }

        private static void Run(Player player, List<Target> targets)
        {
            if (targets.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "$smartchest_msg_no_chest");
                return;
            }

            Inventory playerInv = player.GetInventory();
            int movedTotal = 0;
            var usedChests = new HashSet<Container>();
            var claimed = new HashSet<Container>();

            // Copie de la liste : on modifie l'inventaire pendant la boucle
            // On lit la liste brute de la grille principale (comme Quick Stack Store) : avec Equipment and
            // Quick Slots, GetAllItems() inclut aussi ses emplacements d'équipement et de raccourcis.
            foreach (ItemDrop.ItemData item in playerInv.m_inventory.ToList())
            {
                if (!CanStore(item) || QuickStackStoreCompat.IsFavorited(player, item)) continue;
                // Emplacements d'équipement / raccourcis / carquois ajoutés par d'autres mods d'inventaire
                if (ModCompat.IsProtectedSlot(playerInv, item.m_gridPos)) continue;

                List<string> itemCats = CatalogManager.GetItemCategories(item);

                // 1) coffres qui partagent une catégorie avec l'objet ; ceux qui contiennent déjà cet objet d'abord, puis les plus proches
                IEnumerable<Target> matching = targets
                    .Where(t => itemCats.Any(t.Categories.Contains))
                    .OrderByDescending(t => Contains(t.Inventory, item))
                    .ThenBy(t => t.Distance);

                // 2) puis les coffres trop-plein, si l'objet n'a pas trouvé de place
                IEnumerable<Target> overflow = targets
                    .Where(t => t.Mode == ChestMode.Overflow && !itemCats.Any(t.Categories.Contains))
                    .OrderBy(t => t.Distance);

                foreach (Target t in matching.Concat(overflow))
                {
                    int moved = MoveInto(playerInv, t, item, claimed);
                    if (moved > 0)
                    {
                        movedTotal += moved;
                        usedChests.Add(t.Container);
                    }
                    if (!playerInv.ContainsItem(item)) break;
                }
            }

            // Libère les coffres modifiés à distance
            foreach (Target t in targets)
                if (t.Remote && claimed.Contains(t.Container)) ModCompat.EndRemoteEdit(t.Container);

            string msg = movedTotal == 0
                ? Localization.instance.Localize("$smartchest_msg_nothing")
                : string.Format(Localization.instance.Localize("$smartchest_stored_summary"), movedTotal, usedChests.Count);
            player.Message(MessageHud.MessageType.Center, msg);
        }

        private static bool CanStore(ItemDrop.ItemData item)
        {
            if (item == null || item.m_equipped) return false;
            // Rangée 0 = barre de raccourcis 1 à 8
            if (Plugin.SkipHotbar.Value && item.m_gridPos.y == 0) return false;
            return true;
        }

        private static bool Contains(Inventory inv, ItemDrop.ItemData item)
        {
            foreach (ItemDrop.ItemData i in inv.GetAllItems())
                if (i.m_shared.m_name == item.m_shared.m_name) return true;
            return false;
        }

        /// <summary>Déplace le plus possible de la pile vers le coffre. Renvoie la quantité déplacée.</summary>
        private static int MoveInto(Inventory from, Target t, ItemDrop.ItemData item, HashSet<Container> claimed)
        {
            if (!claimed.Contains(t.Container))
            {
                if (t.Remote) ModCompat.BeginRemoteEdit(t.Container);
                else if (!t.Container.m_nview.IsOwner()) t.Container.m_nview.ClaimOwnership();
                claimed.Add(t.Container);
            }

            int before = item.m_stack;
            t.Inventory.MoveItemToThis(from, item);
            int after = from.ContainsItem(item) ? item.m_stack : 0;
            return before - after;
        }
    }
}
