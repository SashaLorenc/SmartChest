using System.Collections.Generic;
using System.Linq;
using SmartChest.Catalog;

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

        public static bool HasAny(Container container) => Get(container).Count > 0;

        public static ChestMode GetMode(Container container)
        {
            ZDO zdo = GetZdo(container);
            return zdo == null ? ChestMode.Strict : (ChestMode)zdo.GetInt(KeyMode, 0);
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
