using System.Collections.Generic;
using SmartChest.Catalog;

namespace SmartChest.Chests
{
    /// <summary>Description courte d'un coffre, pour son texte de survol et l'alerte « coffre plein ».</summary>
    public static class ChestDescription
    {
        /// <summary>Catégories en couleur, « + le reste » s'il reçoit le reste ; avec extras : priorité et « plein ».</summary>
        public static string Describe(Container container, bool extras)
        {
            List<string> cats = ChestCategories.Get(container);
            string text = CatalogManager.ColoredNames(cats);

            if (ChestCategories.GetMode(container) == ChestMode.Overflow)
            {
                string rest = Localization.instance.Localize(cats.Count == 0 ? "$smartchest_mode_overflow" : "$smartchest_label_rest");
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
