using System;
using System.Collections.Generic;

namespace SmartChest.Catalog
{
    /// <summary>Une catégorie de rangement. Le code (Id) ne change jamais ; le nom et la couleur sont modifiables.</summary>
    [Serializable]
    public class Category
    {
        public string Id;
        /// <summary>Nom affiché. Peut être un jeton de traduction ($smartchest_cat_...) pour les catégories par défaut.</summary>
        public string Name;
        /// <summary>Couleur en hexadécimal, sans # (ex. "E8A33D").</summary>
        public string Color = "FFFFFF";
        public bool IsDefault;
        /// <summary>Une catégorie par défaut ne se supprime pas, elle se masque.</summary>
        public bool Hidden;

        public string DisplayName =>
            Name != null && Name.StartsWith("$") && Localization.instance != null
                ? Localization.instance.Localize(Name)
                : Name ?? Id;
    }

    /// <summary>Catégories choisies à la main pour un type d'objet (remplace les catégories par défaut).</summary>
    [Serializable]
    public class ItemOverride
    {
        public string Prefab;
        public List<string> Categories = new List<string>();
    }

    /// <summary>Contenu du fichier catalog.json. JsonUtility ne gère pas les dictionnaires, d'où les listes.</summary>
    [Serializable]
    public class CatalogData
    {
        public int Version = 1;
        public List<Category> Categories = new List<Category>();
        public List<ItemOverride> ItemOverrides = new List<ItemOverride>();
    }
}
