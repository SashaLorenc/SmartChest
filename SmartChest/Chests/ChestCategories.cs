using System.Collections.Generic;
using System.Linq;
using SmartChest.Catalog;
using UnityEngine;

namespace SmartChest.Chests
{
    public enum ChestMode
    {
        /// <summary>N'accepte que les objets de ses catégories.</summary>
        Strict = 0,
        /// <summary>Accepte en plus tout ce qui n'a trouvé aucun autre coffre.</summary>
        Overflow = 1,
    }

    /// <summary>
    /// Catégories d'un coffre, stockées dans son ZDO : sauvegardées avec le monde
    /// et partagées automatiquement avec les autres joueurs.
    /// </summary>
    public static class ChestCategories
    {
        public const string KeyCategories = "SmartChest_Cats";
        public const string KeyMode = "SmartChest_Mode";
        public const string KeyPriority = "SmartChest_Prio";
        public const int MaxPriority = 9;

        private static ZDO GetZdo(Container container)
        {
            if (container == null || container.m_nview == null || !container.m_nview.IsValid()) return null;
            return container.m_nview.GetZDO();
        }

        /// <summary>Codes des catégories du coffre (les catégories supprimées sont ignorées).</summary>
        public static List<string> Get(Container container)
        {
            ZDO zdo = GetZdo(container);
            if (zdo == null) return new List<string>();
            string raw = zdo.GetString(KeyCategories, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return raw.Split(';')
                .Where(id => id.Length > 0 && CatalogManager.GetCategory(id) != null)
                .Distinct()
                .ToList();
        }

        public static ChestMode GetMode(Container container)
        {
            ZDO zdo = GetZdo(container);
            return zdo == null ? ChestMode.Strict : (ChestMode)zdo.GetInt(KeyMode, 0);
        }

        /// <summary>Priorité de 0 à 9 : entre deux coffres de la même catégorie, le plus prioritaire est rempli d'abord.</summary>
        public static int GetPriority(Container container)
        {
            ZDO zdo = GetZdo(container);
            return zdo == null ? 0 : Mathf.Clamp(zdo.GetInt(KeyPriority, 0), 0, MaxPriority);
        }

        public static bool SetPriority(Container container, int priority)
        {
            ZDO zdo = GetWritableZdo(container);
            if (zdo == null) return false;
            zdo.Set(KeyPriority, Mathf.Clamp(priority, 0, MaxPriority));
            return true;
        }

        /// <summary>Plus aucune case libre (une case = un objet dans la liste de l'inventaire).</summary>
        public static bool IsFull(Container container)
        {
            Inventory inv = container != null ? container.GetInventory() : null;
            return inv != null && inv.m_inventory.Count >= inv.GetWidth() * inv.GetHeight();
        }

        public static bool Set(Container container, IEnumerable<string> categoryIds)
        {
            ZDO zdo = GetWritableZdo(container);
            if (zdo == null) return false;
            zdo.Set(KeyCategories, string.Join(";", categoryIds.Distinct()));
            return true;
        }

        public static bool SetMode(Container container, ChestMode mode)
        {
            ZDO zdo = GetWritableZdo(container);
            if (zdo == null) return false;
            zdo.Set(KeyMode, (int)mode);
            return true;
        }

        public static bool Toggle(Container container, string categoryId, bool enabled)
        {
            var cats = Get(container);
            if (enabled && !cats.Contains(categoryId)) cats.Add(categoryId);
            if (!enabled) cats.Remove(categoryId);
            return Set(container, cats);
        }

        /// <summary>
        /// On n'écrit dans un ZDO que si on en est propriétaire. À l'ouverture d'un coffre,
        /// le jeu donne déjà la propriété au joueur qui l'ouvre ; sinon on la réclame.
        /// </summary>
        private static ZDO GetWritableZdo(Container container)
        {
            ZDO zdo = GetZdo(container);
            if (zdo == null) return null;
            if (!container.m_nview.IsOwner()) container.m_nview.ClaimOwnership();
            return zdo;
        }
    }
}
