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

        private static readonly string[] Palette =
            { "E67E22", "2ECC71", "3498DB", "9B59B6", "E91E63", "1ABC9C", "F1C40F", "E74C3C", "00BCD4", "8BC34A" };

        private static GameObject _buttons;
        private static GameObject _panel;
        private static Container _panelFor;
        private static InputField _nameInput;

        /// <summary>Vrai quand le joueur tape un nom de catégorie : on bloque alors les raccourcis du jeu.</summary>
        public static bool IsTyping => _nameInput != null && _nameInput.isFocused;

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

            AddButton(_buttons.transform, "$smartchest_categories", 0f, 0f, w, h, () =>
            {
                if (IsOpen) Close();
                else Open(InventoryGui.instance.m_currentContainer);
            });
            AddButton(_buttons.transform, "$smartchest_store_here", x2, y2, w, h, () =>
            {
                StoreService.StoreInto(Player.m_localPlayer, InventoryGui.instance.m_currentContainer);
            });
        }

        // ---------- Panneau de catégories ----------

        public static void Open(Container container)
        {
            Close();
            if (container == null) return;
            _panelFor = container;

            List<string> chestCats = ChestCategories.Get(container);
            var visible = new List<Category>();
            foreach (Category c in CatalogManager.Categories)
                if (!c.Hidden) visible.Add(c);

            int rows = (visible.Count + 1) / 2;
            float height = 70f + rows * RowHeight + 150f;

            Transform root = GUIManager.CustomGUIFront != null ? GUIManager.CustomGUIFront.transform : InventoryGui.instance.transform;
            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: root,
                anchorMin: new Vector2(1f, 0.5f),
                anchorMax: new Vector2(1f, 0.5f),
                position: new Vector2(-PanelWidth / 2f - 30f, 0f),
                width: PanelWidth,
                height: height,
                draggable: true);

            AddText(_panel.transform, Localization.instance.Localize("$smartchest_categories") + " : " +
                    Localization.instance.Localize(container.m_name), 20f, 15f, PanelWidth - 40f, 30f, 22,
                GUIManager.Instance.ValheimOrange);

            // Cases à cocher, sur deux colonnes
            for (int i = 0; i < visible.Count; i++)
            {
                Category cat = visible[i];
                float x = 25f + (i % 2) * ColWidth;
                float y = 60f + (i / 2) * RowHeight;

                GameObject toggleGo = GUIManager.Instance.CreateToggle(_panel.transform, 26f, 26f);
                Place(toggleGo, x, y, 26f, 26f);
                Toggle toggle = toggleGo.GetComponent<Toggle>();
                toggle.isOn = chestCats.Contains(cat.Id);
                string id = cat.Id;
                toggle.onValueChanged.AddListener(on => ChestCategories.Toggle(_panelFor, id, on));

                AddText(_panel.transform, $"<color=#{cat.Color}>{cat.DisplayName}</color>", x + 34f, y, ColWidth - 40f, 28f, 18, Color.white);
            }

            float bottom = 60f + rows * RowHeight + 15f;

            // Mode strict / trop-plein
            GameObject modeBtn = null;
            modeBtn = AddButton(_panel.transform, ModeLabel(ChestCategories.GetMode(container)), 25f, bottom, 260f, 34f, () =>
            {
                ChestMode next = ChestCategories.GetMode(_panelFor) == ChestMode.Strict ? ChestMode.Overflow : ChestMode.Strict;
                ChestCategories.SetMode(_panelFor, next);
                SetButtonText(modeBtn, ModeLabel(next));
            });

            AddButton(_panel.transform, "$smartchest_close", PanelWidth - 25f - 150f, bottom, 150f, 34f, Close);

            // Nouvelle catégorie : nom + bouton Ajouter
            float yNew = bottom + 50f;
            GameObject inputGo = GUIManager.Instance.CreateInputField(
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                contentType: InputField.ContentType.Standard,
                placeholderText: Localization.instance.Localize("$smartchest_new_category_placeholder"),
                fontSize: 18,
                width: 330f,
                height: 34f);
            Place(inputGo, 25f, yNew, 330f, 34f);
            _nameInput = inputGo.GetComponent<InputField>();
            _nameInput.characterLimit = 32;

            AddButton(_panel.transform, "$smartchest_new_category", 365f, yNew, 200f, 34f, AddCategoryFromInput);
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

            string color = Palette[CatalogManager.Categories.Count % Palette.Length];
            Category cat = CatalogManager.AddCustomCategory(name, color);
            // La nouvelle catégorie est cochée directement pour ce coffre
            ChestCategories.Toggle(_panelFor, cat.Id, true);
            Open(_panelFor);
        }

        private static string ModeLabel(ChestMode mode) =>
            Localization.instance.Localize("$smartchest_mode") + " : " +
            Localization.instance.Localize(mode == ChestMode.Strict ? "$smartchest_mode_strict" : "$smartchest_mode_overflow");

        // ---------- Petits utilitaires d'interface ----------

        /// <summary>Place un élément depuis le coin haut-gauche de son parent (x vers la droite, y vers le bas).</summary>
        private static void Place(GameObject go, float x, float y, float width, float height)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static GameObject AddButton(Transform parent, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize(label),
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                width: width,
                height: height);
            Place(go, x, y, width, height);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            return go;
        }

        private static void SetButtonText(GameObject button, string text)
        {
            if (button == null) return;
            Text t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        private static void AddText(Transform parent, string text, float x, float y, float width, float height, int size, Color color)
        {
            GameObject go = GUIManager.Instance.CreateText(
                text: text,
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: size,
                color: color,
                outline: true,
                outlineColor: Color.black,
                width: width,
                height: height,
                addContentSizeFitter: false);
            Place(go, x, y, width, height);
            Text t = go.GetComponent<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.alignment = TextAnchor.MiddleLeft;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdatePatch
    {
        /// <summary>Pendant la saisie d'un nom, on empêche l'inventaire de réagir aux touches (E, Tab...).</summary>
        private static bool Prefix() => !ChestPanel.IsTyping;

        private static void Postfix(InventoryGui __instance) => ChestPanel.OnInventoryUpdate(__instance);
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class InventoryGuiHidePatch
    {
        private static void Postfix() => ChestPanel.Close();
    }
}
