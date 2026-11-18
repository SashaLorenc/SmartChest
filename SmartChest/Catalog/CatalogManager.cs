using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace SmartChest.Catalog
{
    /// <summary>
    /// Le catalogue : liste des catégories (par défaut + perso) et catégories de chaque type d'objet.
    /// Sauvegardé dans BepInEx/config/SmartChest/catalog.json.
    /// </summary>
    public static class CatalogManager
    {
        public static string FilePath => Path.Combine(Paths.ConfigPath, "SmartChest", "catalog.json");

        private static CatalogData _data;
        private static readonly Dictionary<string, Category> _byId = new Dictionary<string, Category>();
        private static readonly Dictionary<string, List<string>> _overrides = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, List<string>> _defaultCache = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, int> _rank = new Dictionary<string, int>();

        public static IReadOnlyList<Category> Categories => _data.Categories;

        public static void Load()
        {
            CatalogData loaded = null;
            try
            {
                if (File.Exists(FilePath))
                    loaded = JsonUtility.FromJson<CatalogData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"catalog.json illisible, on repart des valeurs par défaut : {e.Message}");
                TryBackupBrokenFile();
            }

            _data = loaded ?? new CatalogData();
            EnsureDefaultCategories();
            RebuildIndex();
            LoadOverrides();
            Save();
            Plugin.Log.LogInfo($"Catalogue chargé : {_data.Categories.Count} catégories, {_data.ItemOverrides.Count} objets modifiés.");
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                _data.ItemOverrides = _overrides
                    .OrderBy(kv => kv.Key)
                    .Select(kv => new ItemOverride { Prefab = kv.Key, Categories = kv.Value.ToList() })
                    .ToList();
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Impossible d'écrire catalog.json : {e.Message}");
            }
        }

        // ---------- Lecture ----------

        public static Category GetCategory(string id) =>
            id != null && _byId.TryGetValue(id, out var c) ? c : null;

        public static string GetPrefabName(ItemDrop.ItemData item)
        {
            string name = item?.m_dropPrefab != null ? item.m_dropPrefab.name : item?.m_shared?.m_name ?? "";
            int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return clone >= 0 ? name.Substring(0, clone).Trim() : name;
        }

        /// <summary>
        /// Catégories actives d'un objet : celles choisies à la main, sinon celles par défaut.
        /// Les catégories supprimées sont ignorées. Renvoie une copie que l'appelant peut modifier.
        /// </summary>
        public static List<string> GetItemCategories(ItemDrop.ItemData item)
        {
            var result = new List<string>();
            foreach (string id in RawCategories(item))
                if (_byId.ContainsKey(id)) result.Add(id);
            return result;
        }

        /// <summary>L'objet a-t-il au moins une des catégories données ? (sans créer de liste : appelé pour chaque objet à chaque rangement)</summary>
        public static bool MatchesAny(ItemDrop.ItemData item, ICollection<string> categoryIds)
        {
            if (item == null || categoryIds == null || categoryIds.Count == 0) return false;
            foreach (string id in RawCategories(item))
                if (categoryIds.Contains(id) && _byId.ContainsKey(id)) return true;
            return false;
        }

        /// <summary>Rang d'un objet pour le tri : position de sa première catégorie dans le catalogue.</summary>
        public static int SortRank(ItemDrop.ItemData item)
        {
            int best = int.MaxValue;
            foreach (string id in RawCategories(item))
                if (_rank.TryGetValue(id, out int r) && r < best) best = r;
            return best;
        }

        /// <summary>Liste interne (choisie à la main ou calculée et mise en cache) : ne jamais la modifier.</summary>
        private static List<string> RawCategories(ItemDrop.ItemData item)
        {
            string prefab = GetPrefabName(item);
            if (_overrides.TryGetValue(prefab, out var manual)) return manual;
            if (!_defaultCache.TryGetValue(prefab, out var cats))
            {
                cats = DefaultRules.Compute(item);
                _defaultCache[prefab] = cats;
            }
            return cats;
        }

        /// <summary>Texte « Catégorie1, Catégorie2 » avec la couleur de chaque catégorie (balises Unity).</summary>
        public static string ColoredNames(IEnumerable<string> categoryIds) =>
            string.Join(", ", categoryIds
                .Select(GetCategory)
                .Where(c => c != null)
                .Select(c => $"<color=#{c.Color}>{c.DisplayName}</color>"));

        /// <summary>Catégories visibles dans les listes de cases à cocher (non masquées), dans l'ordre du catalogue.</summary>
        public static List<Category> VisibleCategories() => _data.Categories.Where(c => !c.Hidden).ToList();

        public static bool IsOverridden(ItemDrop.ItemData item) => _overrides.ContainsKey(GetPrefabName(item));

        // ---------- Modification (depuis l'interface) ----------

        public static void SetItemCategories(ItemDrop.ItemData item, IEnumerable<string> categoryIds)
        {
            _overrides[GetPrefabName(item)] = categoryIds.Where(_byId.ContainsKey).Distinct().ToList();
            Save();
        }

        public static void ResetItemCategories(ItemDrop.ItemData item)
        {
            if (_overrides.Remove(GetPrefabName(item))) Save();
        }

        public static Category AddCustomCategory(string name, string color)
        {
            string baseId = "perso_" + Slug(name);
            string id = baseId;
            for (int i = 2; _byId.ContainsKey(id); i++) id = baseId + "_" + i;

            var cat = new Category { Id = id, Name = name, Color = color ?? "FFFFFF", IsDefault = false };
            _data.Categories.Add(cat);
            RebuildIndex();
            Save();
            return cat;
        }

        public static void UpdateCategory(string id, string newName, string newColor)
        {
            var cat = GetCategory(id);
            if (cat == null) return;
            if (!string.IsNullOrWhiteSpace(newName)) cat.Name = newName;
            if (!string.IsNullOrWhiteSpace(newColor)) cat.Color = newColor;
            Save();
        }

        /// <summary>Masque ou réaffiche une catégorie (elle disparaît des listes de cases à cocher).</summary>
        public static void SetHidden(string id, bool hidden)
        {
            var cat = GetCategory(id);
            if (cat == null || cat.Hidden == hidden) return;
            cat.Hidden = hidden;
            Save();
        }

        /// <summary>Supprime une catégorie perso, ou masque une catégorie par défaut.</summary>
        public static void RemoveCategory(string id)
        {
            var cat = GetCategory(id);
            if (cat == null) return;
            if (cat.IsDefault)
            {
                cat.Hidden = true;
            }
            else
            {
                _data.Categories.Remove(cat);
                foreach (var list in _overrides.Values) list.Remove(id);
                RebuildIndex();
            }
            Save();
        }

        // ---------- Interne ----------

        private static void EnsureDefaultCategories()
        {
            foreach (var def in DefaultRules.CreateDefaultCategories())
            {
                if (!_data.Categories.Any(c => c.Id == def.Id))
                    _data.Categories.Add(def);
            }
        }

        private static void RebuildIndex()
        {
            _byId.Clear();
            _rank.Clear();
            foreach (var c in _data.Categories)
                if (!string.IsNullOrEmpty(c.Id) && !_byId.ContainsKey(c.Id))
                {
                    _byId[c.Id] = c;
                    _rank[c.Id] = _rank.Count;
                }
        }

        private static void LoadOverrides()
        {
            _overrides.Clear();
            foreach (var o in _data.ItemOverrides)
                if (!string.IsNullOrEmpty(o.Prefab))
                    _overrides[o.Prefab] = o.Categories ?? new List<string>();
        }

        private static string Slug(string name)
        {
            var chars = (name ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).Take(24).ToArray();
            return chars.Length > 0 ? new string(chars) : "cat";
        }

        private static void TryBackupBrokenFile()
        {
            try { File.Copy(FilePath, FilePath + ".broken", true); } catch { /* rien à faire */ }
        }
    }
}
