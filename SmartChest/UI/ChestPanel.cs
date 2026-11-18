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
    /// Boutons « Catégories » et « Ranger ici » au-dessus du coffre ouvert,
    /// et panneau de cases à cocher pour choisir les catégories du coffre.
    /// </summary>
    /// <summary>Où placer les boutons autour du coffre ouvert (réglable dans la config).</summary>
    public enum ButtonPlacement
    {
        Dessus,
        Dessous,
        Gauche,
        Droite,
    }

    public static class ChestPanel
    {
        private const float RowHeight = 34f;
        private const float ColWidth = 270f;
        private const float PanelWidth = 590f;

        private static GameObject _buttons;
        private static GameObject _panel;
        private static Container _panelFor;
        private static InputField _nameInput;

        /// <summary>Vrai quand le joueur tape dans un de nos champs : on bloque alors les raccourcis du jeu.</summary>
        public static bool IsTyping => UiKit.IsTyping;

        public static bool IsOpen => _panel != null;

        // ---------- Boutons au-dessus du coffre ----------

        internal static void OnInventoryUpdate(InventoryGui gui)
        {
            Container current = gui.IsContainerOpen() ? gui.m_currentContainer : null;
            bool show = current != null && current.GetComponent<TombStone>() == null;

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
            ButtonPlacement place = Plugin.ButtonsPlacement.Value;
            bool vertical = place == ButtonPlacement.Gauche || place == ButtonPlacement.Droite;
            float w = Plugin.ButtonsWidth.Value;
            const float h = 30f;
            const float gap = 6f;

            _buttons = new GameObject("SmartChestButtons", typeof(RectTransform));
            var rt = (RectTransform)_buttons.transform;
            rt.SetParent(gui.m_container, false);
            rt.sizeDelta = vertical ? new Vector2(w, h * 2 + gap) : new Vector2(w * 2 + gap, h);

            var offset = new Vector2(Plugin.ButtonsOffsetX.Value, Plugin.ButtonsOffsetY.Value);
            switch (place)
            {
                case ButtonPlacement.Dessus:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(0f, gap) + offset;
                    break;
                case ButtonPlacement.Dessous:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -gap) + offset;
                    break;
                case ButtonPlacement.Gauche:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-gap, 0f) + offset;
                    break;
                default: // Droite
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(gap, 0f) + offset;
                    break;
            }

            float x2 = vertical ? 0f : w + gap;
            float y2 = vertical ? h + gap : 0f;

            UiKit.AddButton(_buttons.transform, "$smartchest_categories", 0f, 0f, w, h, () =>
            {
                if (IsOpen) Close();
                else Open(InventoryGui.instance.m_currentContainer);
            });
            UiKit.AddButton(_buttons.transform, "$smartchest_store_here", x2, y2, w, h, () =>
            {
                StoreService.StoreInto(Player.m_localPlayer, InventoryGui.instance.m_currentContainer);
            });
        }

        // ---------- Panneau de catégories ----------

        public static void Open(Container container)
        {
            Close();
            ItemEditorPanel.Close();
            CategoryManagerPanel.Close();
            if (container == null) return;
            _panelFor = container;

            List<string> chestCats = ChestCategories.Get(container);
            var visible = new List<Category>();
            foreach (Category c in CatalogManager.Categories)
                if (!c.Hidden) visible.Add(c);

            int rows = (visible.Count + 1) / 2;
            float height = 70f + rows * RowHeight + 150f;

            _panel = UiKit.CreatePanel(PanelWidth, height);

            UiKit.AddText(_panel.transform, Localization.instance.Localize("$smartchest_categories") + " : " +
                    Localization.instance.Localize(container.m_name), 20f, 15f, PanelWidth - 40f, 30f, 22,
                GUIManager.Instance.ValheimOrange);

            // Cases à cocher, sur deux colonnes
            for (int i = 0; i < visible.Count; i++)
            {
                Category cat = visible[i];
                float x = 25f + (i % 2) * ColWidth;
                float y = 60f + (i / 2) * RowHeight;

                string id = cat.Id;
                UiKit.AddToggle(_panel.transform, x, y, chestCats.Contains(id), on => ChestCategories.Toggle(_panelFor, id, on));

                UiKit.AddText(_panel.transform, $"<color=#{cat.Color}>{cat.DisplayName}</color>", x + 34f, y, ColWidth - 40f, 28f, 18, Color.white);
            }

            float bottom = 60f + rows * RowHeight + 15f;

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

            // Nouvelle catégorie : nom + bouton Ajouter
            float yNew = bottom + 50f;
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
            ItemEditorPanel.OnInventoryUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class InventoryGuiHidePatch
    {
        private static void Postfix()
        {
            ChestPanel.Close();
            ItemEditorPanel.Close();
            CategoryManagerPanel.Close();
        }
    }
}
