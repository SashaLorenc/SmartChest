using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using SmartChest.Catalog;
using SmartChest.Chests;
using SmartChest.Storing;
using UnityEngine;
using UnityEngine.UI;

namespace SmartChest.UI
{
    /// <summary>
    /// Boutons « Catégories », « Ranger ici » et « Trier » à côté du coffre ouvert,
    /// et panneau de cases à cocher pour choisir les catégories du coffre.
    /// </summary>
    public static class ChestPanel
    {
        private const float PanelWidth = UiKit.CategoryGridWidth;

        private static GameObject _buttons;
        private static GameObject _panel;
        private static Container _panelFor;
        private static InputField _nameInput;
        private static Container _checkedContainer;
        private static bool _checkedEligible;

        public static bool IsOpen => _panel != null;

        // ---------- Boutons à côté du coffre ----------

        internal static void OnInventoryUpdate(InventoryGui gui)
        {
            Container current = gui.IsContainerOpen() ? gui.m_currentContainer : null;
            // Pas de boutons sur les sacs à dos, tombes, tas à objet unique, coffres de donjon...
            // (vérifié une fois par coffre ouvert, pas à chaque image)
            if (current != _checkedContainer)
            {
                _checkedContainer = current;
                _checkedEligible = current != null && Compat.ModCompat.IsEligibleChest(current);
            }
            bool show = current != null && _checkedEligible;

            if (show && _buttons == null) CreateButtons(gui);
            if (_buttons != null && _buttons.activeSelf != show) _buttons.SetActive(show);

            if (_panel != null && (current == null || current != _panelFor)) Close();
        }

        /// <summary>Recrée les boutons au prochain affichage (après un changement de config).</summary>
        public static void ResetButtons()
        {
            if (_buttons != null) Object.Destroy(_buttons);
            _buttons = null;
        }

        private static void CreateButtons(InventoryGui gui)
        {
            _buttons = UiKit.CreateButtonBar(gui.m_container, "SmartChestButtons", Plugin.ButtonsPlacement.Value,
                new Vector2(Plugin.ButtonsOffsetX.Value, Plugin.ButtonsOffsetY.Value), Plugin.ButtonsWidth.Value,
                new[] { "$smartchest_categories", "$smartchest_store_here", "$smartchest_sort" },
                new UnityEngine.Events.UnityAction[]
                {
                    () =>
                    {
                        if (IsOpen) Close();
                        else Open(InventoryGui.instance.m_currentContainer);
                    },
                    () => StoreService.StoreInto(Player.m_localPlayer, InventoryGui.instance.m_currentContainer),
                    () => SortService.SortChest(Player.m_localPlayer, InventoryGui.instance.m_currentContainer),
                });
        }

        // ---------- Panneau de catégories ----------

        public static void Open(Container container)
        {
            Panels.CloseAll();
            if (container == null) return;
            _panelFor = container;

            float gridHeight = UiKit.CategoryGridHeight();
            float height = 70f + gridHeight + 200f;

            _panel = UiKit.CreatePanel(PanelWidth, height);

            UiKit.AddText(_panel.transform, Localization.instance.Localize("$smartchest_categories") + " : " +
                    Localization.instance.Localize(container.m_name), 20f, 15f, PanelWidth - 40f, 30f, 22,
                GUIManager.Instance.ValheimOrange);

            UiKit.AddCategoryGrid(_panel.transform, 60f, ChestCategories.Get(container), (id, on) => ChestCategories.Toggle(_panelFor, id, on));

            float bottom = 60f + gridHeight + 15f;

            // Mode strict / trop-plein
            GameObject modeBtn = null;
            modeBtn = UiKit.AddButton(_panel.transform, ModeLabel(ChestCategories.GetMode(container)), 25f, bottom, 260f, 34f, () =>
            {
                ChestMode next = ChestCategories.GetMode(_panelFor) == ChestMode.Strict ? ChestMode.Overflow : ChestMode.Strict;
                ChestCategories.SetMode(_panelFor, next);
                UiKit.SetButtonText(modeBtn, ModeLabel(next));
            });

            UiKit.AddButton(_panel.transform, "$smartchest_manage", 295f, bottom, 140f, 34f, () =>
            {
                Container c = _panelFor;
                Close();
                CategoryManagerPanel.Open(c);
            });
            UiKit.AddButton(_panel.transform, "$smartchest_close", PanelWidth - 25f - 120f, bottom, 120f, 34f, Close);

            // Priorité : entre deux coffres de la même catégorie, le plus prioritaire est rempli d'abord
            float yPrio = bottom + 48f;
            GameObject prioBtn = UiKit.AddButton(_panel.transform, PriorityLabel(ChestCategories.GetPriority(container)), 25f, yPrio, 260f, 34f, () => { });
            prioBtn.GetComponent<Button>().interactable = false;
            UiKit.AddButton(_panel.transform, "-", 295f, yPrio, 45f, 34f, () => ChangePriority(prioBtn, -1));
            UiKit.AddButton(_panel.transform, "+", 345f, yPrio, 45f, 34f, () => ChangePriority(prioBtn, +1));

            // Nouvelle catégorie : nom + bouton Ajouter
            float yNew = yPrio + 48f;
            _nameInput = UiKit.AddInput(_panel.transform, "", Localization.instance.Localize("$smartchest_new_category_placeholder"), 25f, yNew, 330f, 34f);

            UiKit.AddButton(_panel.transform, "$smartchest_new_category", 365f, yNew, 200f, 34f, AddCategoryFromInput);
        }

        public static void Close()
        {
            if (_panel != null) Object.Destroy(_panel);
            _panel = null;
            _panelFor = null;
            _nameInput = null;
        }

        private static void AddCategoryFromInput()
        {
            if (_nameInput == null || _panelFor == null) return;
            string name = _nameInput.text.Trim();
            if (name.Length == 0) return;

            string color = UiKit.Palette[CatalogManager.Categories.Count % UiKit.Palette.Length];
            Category cat = CatalogManager.AddCustomCategory(name, color);
            // La nouvelle catégorie est cochée directement pour ce coffre
            ChestCategories.Toggle(_panelFor, cat.Id, true);
            Open(_panelFor);
        }

        private static void ChangePriority(GameObject label, int delta)
        {
            ChestCategories.SetPriority(_panelFor, ChestCategories.GetPriority(_panelFor) + delta);
            UiKit.SetButtonText(label, PriorityLabel(ChestCategories.GetPriority(_panelFor)));
        }

        private static string PriorityLabel(int prio) =>
            Localization.instance.Localize("$smartchest_priority") + " : " + prio;

        private static string ModeLabel(ChestMode mode) =>
            Localization.instance.Localize("$smartchest_mode") + " : " +
            Localization.instance.Localize(mode == ChestMode.Strict ? "$smartchest_mode_strict" : "$smartchest_mode_overflow");
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdatePatch
    {
        /// <summary>Pendant la saisie d'un nom, on empêche l'inventaire de réagir aux touches (E, Tab...).</summary>
        private static bool Prefix() => !UiKit.IsTyping;

        private static void Postfix(InventoryGui __instance)
        {
            ChestPanel.OnInventoryUpdate(__instance);
            InventoryButtons.OnInventoryUpdate(__instance);
            ItemEditorPanel.OnInventoryUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class InventoryGuiHidePatch
    {
        private static void Postfix() => Panels.CloseAll();
    }
}
