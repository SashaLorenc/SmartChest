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
            string desc = ChestDescription.Describe(__instance, true);
            if (desc.Length > 0) __result += "\n" + desc;
        }
    }
}

namespace SmartChest.UI
{
    /// <summary>Description courte d'un coffre, pour son texte de survol et l'alerte « coffre plein ».</summary>
    public static class ChestDescription
    {
        /// <summary>Catégories en couleur, « + le reste » s'il reçoit le reste ; avec extras : priorité et « plein ».</summary>
        public static string Describe(Container container, bool extras)
        {
            System.Collections.Generic.List<string> parts = ChestCategories.Get(container)
                .Select(CatalogManager.GetCategory)
                .Where(c => c != null)
                .Select(c => $"<color=#{c.Color}>{c.DisplayName}</color>")
                .ToList();

            string text = string.Join(", ", parts);
            if (ChestCategories.GetMode(container) == ChestMode.Overflow)
            {
                string rest = Localization.instance.Localize(parts.Count == 0 ? "$smartchest_mode_overflow" : "$smartchest_label_rest");
                text += (text.Length > 0 ? " " : "") + "<color=#CCCCCC>" + rest + "</color>";
            }
            if (!extras || text.Length == 0) return text;

            int prio = ChestCategories.GetPriority(container);
            if (prio > 0) text += " <color=#CCCCCC>(" + Localization.instance.Localize("$smartchest_priority") + " " + prio + ")</color>";
            if (ChestCategories.IsFull(container))
                text += " <color=#FF5050>" + Localization.instance.Localize("$smartchest_full").ToUpperInvariant() + "</color>";
            return text;
        }
    }
}
