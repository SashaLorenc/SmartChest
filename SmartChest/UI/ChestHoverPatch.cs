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
            if (!ChestCategories.IsSmart(__instance)) return;
            string desc = ChestLabel.Describe(__instance, true);
            if (desc.Length > 0) __result += "\n" + desc;
        }
    }
}
