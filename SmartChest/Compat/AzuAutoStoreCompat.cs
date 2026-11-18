using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SmartChest.Catalog;
using SmartChest.Chests;

namespace SmartChest.Compat
{
    /// <summary>
    /// Intégration optionnelle avec AzuAutoStore (rangement automatique quand on passe près d'un coffre,
    /// et coffres qui aspirent les objets au sol).
    ///
    /// AzuAutoStore vérifie chaque coffre avec des méthodes TryStore(Container, ref ItemData, bool).
    /// Sur un coffre catégorisé :
    /// - un objet de ses catégories y est rangé, même si le coffre n'en contient pas encore
    ///   (alors qu'AzuAutoStore exige souvent que l'objet y soit déjà) ;
    /// - en mode strict, les autres objets sont refusés ;
    /// - en mode trop-plein, AzuAutoStore garde son comportement normal pour les autres objets.
    /// On cherche ces méthodes par leur signature, pour survivre aux réorganisations de son code.
    /// </summary>
    public static class AzuAutoStoreCompat
    {
        public static void TryInit(Harmony harmony)
        {
            if (!ModCompat.Has(ModCompat.AzuAutoStore)) return;

            try
            {
                Assembly asm = BepInEx.Bootstrap.Chainloader.PluginInfos[ModCompat.AzuAutoStore].Instance.GetType().Assembly;
                var targets = new List<MethodInfo>();
                foreach (Type t in AccessTools.GetTypesFromAssembly(asm))
                {
                    foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (m.Name != "TryStore" || m.ReturnType != typeof(bool)) continue;
                        ParameterInfo[] p = m.GetParameters();
                        if (p.Length >= 2 && p[0].ParameterType == typeof(Container)
                            && p[1].ParameterType == typeof(ItemDrop.ItemData).MakeByRefType())
                            targets.Add(m);
                    }
                }

                var prefix = new HarmonyMethod(typeof(AzuAutoStoreCompat), nameof(TryStorePrefix));
                foreach (MethodInfo m in targets) harmony.Patch(m, prefix: prefix);

                if (targets.Count > 0)
                    Plugin.Log.LogInfo($"Intégration AzuAutoStore : active ({targets.Count} méthode(s)).");
                else
                    Plugin.Log.LogWarning("AzuAutoStore : méthode TryStore introuvable, intégration inactive.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Intégration AzuAutoStore désactivée : " + e.Message);
            }
        }

        // Paramètres par position : __0 = le coffre, __1 = l'objet (ref)
        private static bool TryStorePrefix(Container __0, ref ItemDrop.ItemData __1, ref bool __result)
        {
            if (!Plugin.AutoStoreIntegration.Value || __0 == null || __1 == null) return true;

            List<string> chestCats = ChestCategories.Get(__0);
            if (chestCats.Count == 0) return true;

            bool matches = CatalogManager.GetItemCategories(__1).Any(chestCats.Contains);
            if (!matches)
            {
                if (ChestCategories.GetMode(__0) == ChestMode.Overflow) return true; // comportement normal
                __result = false;
                return false;
            }

            __result = StoreCopy(__0, __1);
            return false;
        }

        /// <summary>Même façon de ranger qu'AzuAutoStore : on retire de la pile d'origine ce qui entre dans le coffre.</summary>
        private static bool StoreCopy(Container chest, ItemDrop.ItemData item)
        {
            Inventory inv = chest.GetInventory();
            if (inv == null || item.m_stack <= 0) return false;

            bool changed = false;
            while (item.m_stack > 0 && inv.CanAddItem(item, 1))
            {
                ItemDrop.ItemData one = item.Clone();
                one.m_stack = 1;
                if (!inv.AddItem(one)) break;
                item.m_stack--;
                changed = true;
            }

            if (changed) chest.Save();
            return changed;
        }
    }
}
