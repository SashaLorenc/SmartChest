using System.Linq;
using HarmonyLib;
using SmartChest.Catalog;
using SmartChest.Chests;

namespace SmartChest.UI
{
    /// <summary>Affiche les catégories d'un coffre dans son texte de survol, sans avoir à l'ouvrir.</summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class ChestHoverPatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (__result == null) return;
            var cats = ChestCategories.Get(__instance);
            if (cats.Count == 0) return;

            string list = string.Join(", ", cats
                .Select(CatalogManager.GetCategory)
                .Where(c => c != null)
                .Select(c => $"<color=#{c.Color}>{c.DisplayName}</color>"));
            if (ChestCategories.GetMode(__instance) == ChestMode.Overflow)
                list += " <color=#AAAAAA>(" + Localization.instance.Localize("$smartchest_mode_overflow") + ")</color>";

            __result += "\n" + list;
        }
    }
}
