using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using SmartChest.Catalog;
using UnityEngine;

namespace SmartChest.UI
{
    /// <summary>
    /// Éditeur des catégories d'un type d'objet : dans l'inventaire, survoler un objet
    /// et appuyer sur la touche (K par défaut). Le changement vaut pour tous les objets de ce type.
    /// </summary>
    public static class ItemEditorPanel
    {
        private const float RowHeight = 34f;
        private const float ColWidth = 270f;
        private const float PanelWidth = 590f;

        private static GameObject _panel;
        private static ItemDrop.ItemData _item;

        public static bool IsOpen => _panel != null;

        /// <summary>Dernière case d'inventaire survolée, signalée par l'interface du jeu elle-même (UIInputHandler).</summary>
        private static GameObject _hoveredSlot;

        internal static void SetHovered(GameObject go) => _hoveredSlot = go;

        internal static void ClearHovered(GameObject go)
        {
            if (_hoveredSlot == go) _hoveredSlot = null;
        }

        internal static void OnInventoryUpdate(InventoryGui gui)
        {
            if (UiKit.IsTyping || !Plugin.EditItemKey.Value.IsDown()) return;

            ItemDrop.ItemData hovered = GetHoveredItem(gui.m_playerGrid, "joueur") ?? GetHoveredItem(gui.m_containerGrid, "coffre");
            if (hovered == null)
            {
                Plugin.Log.LogInfo($"Touche K : aucun objet sous la souris (case survolée : {(_hoveredSlot != null ? _hoveredSlot.name : "aucune")}).");
                return;
            }
            Plugin.Log.LogInfo($"Touche K : édition de {CatalogManager.GetPrefabName(hovered)}.");
            Open(hovered);
        }

        /// <summary>
        /// Objet sous la souris dans une grille d'inventaire.
        /// Chaque case du jeu porte un UIInputHandler qui reçoit les événements « souris entre / sort » :
        /// on retient la case survolée (patch plus bas), puis InventoryGrid.GetButtonPos(case) donne sa position.
        /// Rien ne dépend de la position de la souris ni des champs internes des cases.
        /// </summary>
        private static ItemDrop.ItemData GetHoveredItem(InventoryGrid grid, string label)
        {
            if (grid == null || grid.GetInventory() == null || _hoveredSlot == null) return null;
            try
            {
                // La case survolée peut être un enfant de l'objet que la grille connaît : on remonte les parents
                for (Transform t = _hoveredSlot.transform; t != null; t = t.parent)
                {
                    Vector2i pos = grid.GetButtonPos(t.gameObject);
                    if (pos.x < 0 || pos.y < 0) continue;
                    ItemDrop.ItemData item = grid.GetInventory().GetItemAt(pos.x, pos.y);
                    Plugin.Log.LogInfo($"Touche K : case ({pos.x},{pos.y}) de l'inventaire {label}, objet : {(item != null ? CatalogManager.GetPrefabName(item) : "vide")}.");
                    return item;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Touche K : erreur en lisant l'inventaire {label} : {ex}");
            }
            return null;
        }

        public static void Open(ItemDrop.ItemData item)
        {
            Close();
            ChestPanel.Close();
            SearchPanel.Close();
            CategoryManagerPanel.Close();
            if (item == null) return;
            _item = item;

            List<string> itemCats = CatalogManager.GetItemCategories(item);
            var visible = new List<Category>();
            foreach (Category c in CatalogManager.Categories)
                if (!c.Hidden) visible.Add(c);

            int rows = (visible.Count + 1) / 2;
            float height = 110f + rows * RowHeight + 70f;
            _panel = UiKit.CreatePanel(PanelWidth, height);

            string itemName = Localization.instance.Localize(item.m_shared.m_name);
            UiKit.AddText(_panel.transform, Localization.instance.Localize("$smartchest_categories") + " : " + itemName,
                20f, 15f, PanelWidth - 40f, 30f, 22, GUIManager.Instance.ValheimOrange);

            string state = CatalogManager.IsOverridden(item) ? "$smartchest_item_custom" : "$smartchest_item_default";
            UiKit.AddText(_panel.transform, Localization.instance.Localize(state), 20f, 48f, PanelWidth - 40f, 26f, 16,
                new Color(0.8f, 0.8f, 0.8f));

            for (int i = 0; i < visible.Count; i++)
            {
                Category cat = visible[i];
                float x = 25f + (i % 2) * ColWidth;
                float y = 90f + (i / 2) * RowHeight;
                string id = cat.Id;

                UiKit.AddToggle(_panel.transform, x, y, itemCats.Contains(id), on => SetCategory(id, on));
                UiKit.AddText(_panel.transform, $"<color=#{cat.Color}>{cat.DisplayName}</color>", x + 34f, y, ColWidth - 40f, 28f, 18, Color.white);
            }

            float bottom = 90f + rows * RowHeight + 15f;
            UiKit.AddButton(_panel.transform, "$smartchest_reset_default", 25f, bottom, 260f, 34f, () =>
            {
                ItemDrop.ItemData it = _item;
                CatalogManager.ResetItemCategories(it);
                Open(it);
            });
            UiKit.AddButton(_panel.transform, "$smartchest_close", PanelWidth - 25f - 150f, bottom, 150f, 34f, Close);
        }

        private static void SetCategory(string id, bool on)
        {
            if (_item == null) return;
            List<string> cats = CatalogManager.GetItemCategories(_item);
            if (on && !cats.Contains(id)) cats.Add(id);
            if (!on) cats.Remove(id);
            CatalogManager.SetItemCategories(_item, cats);
        }

        public static void Close()
        {
            if (_panel != null) Object.Destroy(_panel);
            _panel = null;
            _item = null;
        }
    }
}
