using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using SmartChest.Catalog;

namespace SmartChest.UI
{
    /// <summary>
    /// Ajoute une ligne « Catégories : ... » à l'infobulle des objets.
    /// La signature de ItemData.GetTooltip change selon les versions du jeu : on patche toutes
    /// les surcharges statiques dont le premier paramètre est un ItemData.
    /// </summary>
    [HarmonyPatch]
    internal static class TooltipPatch
    {
        private const string Marker = "<!--sc-->";

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(ItemDrop.ItemData)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "GetTooltip"
                            && m.ReturnType == typeof(string)
                            && m.GetParameters().Length > 0
                            && m.GetParameters()[0].ParameterType == typeof(ItemDrop.ItemData));
        }

        private static void Postfix(ref string __result, ItemDrop.ItemData __0)
        {
            if (!Plugin.ShowCategoriesInTooltip.Value || __0 == null || __result == null) return;
            // Une surcharge peut appeler l'autre : on n'ajoute la ligne qu'une fois
            if (__result.Contains(Marker)) return;

            List<string> ids = CatalogManager.GetItemCategories(__0);
            if (ids.Count == 0) return;

            var sb = new StringBuilder(__result);
            sb.Append("\n").Append(Localization.instance.Localize("$smartchest_tooltip_categories")).Append(" : ");
            sb.Append(CatalogManager.ColoredNames(ids));
            if (CatalogManager.IsOverridden(__0)) sb.Append(" *");
            sb.Append($"\n<color=#AAAAAA>[{Plugin.EditItemKey.Value}] {Localization.instance.Localize("$smartchest_tooltip_edit_hint")}</color>");
            sb.Append(Marker);
            __result = sb.ToString();
        }
    }
}
