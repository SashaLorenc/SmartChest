using HarmonyLib;
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
            // Vide si le coffre n'a ni catégorie ni mode « reçoit le reste »
            string desc = ChestDescription.Describe(__instance, true);
            if (desc.Length > 0) __result += "\n" + desc;
        }
    }
}
