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
        /// <summary>Un coffre candidat, lu une seule fois avant le rangement.</summary>
        private class Target
        {
            public Container Container;
            public Inventory Inventory;
            public HashSet<string> Categories;
            public ChestMode Mode;
            public int Priority;
            public float Distance;
            /// <summary>Coffre modifié à distance (pas celui que le joueur a ouvert).</summary>
            public bool Remote;
            /// <summary>Propriété réclamée / coffre marqué « en cours d'utilisation » par nous.</summary>
            public bool Claimed;
        }

        /// <summary>Bilan d'un rangement, pour les messages au joueur.</summary>
        private class Report
        {
            public int Moved;
            public readonly HashSet<Container> UsedChests = new HashSet<Container>();
            /// <summary>Coffres catégorisés qui n'ont pas pu tout prendre faute de place.</summary>
            public readonly List<Container> FullChests = new List<Container>();
        }

        /// <summary>Range dans tous les coffres catégorisés à portée (touche L, bouton de l'inventaire).</summary>
        public static void StoreNearby(Player player)
        {
            if (player == null) return;
            Container open = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;
            Vector3 pos = player.transform.position;
            float radius = Plugin.StoreRadius.Value;

            var targets = new List<Target>();
            foreach (Container c in ContainerRegistry.All())
            {
                float dist = Vector3.Distance(pos, c.transform.position);
                if (dist > radius) continue;
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
            ChestMode mode = ChestCategories.GetMode(c);
            // Sans catégorie, seul un coffre « reçoit le reste » participe
            if (cats.Count == 0 && mode != ChestMode.Overflow) return null;

            // Coffre ouvert par un autre joueur, coffre privé, protégé par une balise, bateau en multi... : on n'y touche pas
            if (!isOpenByMe && !ModCompat.CanAffectRemotely(c, player)) return null;

            return new Target
            {
                Container = c,
                Inventory = inv,
                Categories = new HashSet<string>(cats),
                Mode = mode,
                Priority = ChestCategories.GetPriority(c),
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
            var report = new Report();

            // Copie de la liste : on modifie l'inventaire pendant la boucle.
            // On lit la liste brute de la grille principale (comme Quick Stack Store) : avec Equipment and
            // Quick Slots, GetAllItems() inclut aussi ses emplacements d'équipement et de raccourcis.
            foreach (ItemDrop.ItemData item in playerInv.m_inventory.ToList())
            {
                if (CanStore(player, playerInv, item)) StoreItem(playerInv, item, targets, report);
            }

            // Libère les coffres modifiés à distance
            foreach (Target t in targets)
                if (t.Remote && t.Claimed) ModCompat.EndRemoteEdit(t.Container);

            ShowReport(player, report);
        }

        /// <summary>Range un objet : d'abord dans les coffres de ses catégories, puis dans ceux qui reçoivent le reste.</summary>
        private static void StoreItem(Inventory playerInv, ItemDrop.ItemData item, List<Target> targets, Report report)
        {
            var matching = new List<Target>();
            var overflow = new List<Target>();
            foreach (Target t in targets)
            {
                if (CatalogManager.MatchesAny(item, t.Categories)) matching.Add(t);
                else if (t.Mode == ChestMode.Overflow) overflow.Add(t);
            }

            // Priorité la plus haute d'abord, puis les coffres qui contiennent déjà cet objet, puis les plus proches
            IEnumerable<Target> order = matching
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => Contains(t.Inventory, item))
                .ThenBy(t => t.Distance)
                .Concat(overflow
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.Distance));

            foreach (Target t in order)
            {
                int moved = MoveInto(playerInv, t, item);
                if (moved > 0)
                {
                    report.Moved += moved;
                    report.UsedChests.Add(t.Container);
                }
                if (!playerInv.ContainsItem(item)) return;
            }

            // L'objet reste dans l'inventaire alors qu'il avait des coffres de sa catégorie : ils sont pleins
            foreach (Target t in matching)
                if (!report.FullChests.Contains(t.Container) && ChestCategories.IsFull(t.Container))
                    report.FullChests.Add(t.Container);
        }

        private static void ShowReport(Player player, Report report)
        {
            string msg = report.Moved == 0
                ? Localization.instance.Localize("$smartchest_msg_nothing")
                : string.Format(Localization.instance.Localize("$smartchest_stored_summary"), report.Moved, report.UsedChests.Count);
            player.Message(MessageHud.MessageType.Center, msg);

            if (!Plugin.FullChestAlert.Value) return;
            string fullFormat = Localization.instance.Localize("$smartchest_msg_full");
            foreach (Container c in report.FullChests)
                player.Message(MessageHud.MessageType.TopLeft, string.Format(fullFormat, ChestDescription.Describe(c, false)));
        }

        /// <summary>Objets qui ne bougent jamais : équipés, barre de raccourcis, favoris QSS, emplacements spéciaux d'autres mods.</summary>
        private static bool CanStore(Player player, Inventory playerInv, ItemDrop.ItemData item)
        {
            if (item == null || item.m_equipped) return false;
            // Rangée 0 = barre de raccourcis 1 à 8
            if (Plugin.SkipHotbar.Value && item.m_gridPos.y == 0) return false;
            if (QuickStackStoreCompat.IsFavorited(player, item)) return false;
            // Emplacements d'équipement / raccourcis / carquois ajoutés par d'autres mods d'inventaire
            return !ModCompat.IsProtectedSlot(playerInv, item.m_gridPos);
        }

        private static bool Contains(Inventory inv, ItemDrop.ItemData item)
        {
            foreach (ItemDrop.ItemData i in inv.m_inventory)
                if (i.m_shared.m_name == item.m_shared.m_name) return true;
            return false;
        }

        /// <summary>Déplace le plus possible de la pile vers le coffre. Renvoie la quantité déplacée.</summary>
        private static int MoveInto(Inventory from, Target t, ItemDrop.ItemData item)
        {
            if (!t.Claimed)
            {
                if (t.Remote) ModCompat.BeginRemoteEdit(t.Container);
                else if (!t.Container.m_nview.IsOwner()) t.Container.m_nview.ClaimOwnership();
                t.Claimed = true;
            }

            int before = item.m_stack;
            t.Inventory.MoveItemToThis(from, item);
            int after = from.ContainsItem(item) ? item.m_stack : 0;
            return before - after;
        }
    }
}
