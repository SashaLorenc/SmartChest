using System.Collections;
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

        internal static void OnInventoryUpdate(InventoryGui gui)
        {
            if (UiKit.IsTyping || !Plugin.EditItemKey.Value.IsDown()) return;

            ItemDrop.ItemData hovered = GetHoveredItem(gui.m_playerGrid) ?? GetHoveredItem(gui.m_containerGrid);
            if (hovered != null) Open(hovered);
        }

        /// <summary>
        /// Objet sous la souris dans une grille d'inventaire. Le type interne des cases (InventoryGrid.Element)
        /// n'est pas accessible selon les versions, donc on passe par la réflexion (Traverse) :
        /// d'abord GetHoveredElement(), sinon on cherche la case qui contient la souris.
        /// </summary>
        private static ItemDrop.ItemData GetHoveredItem(InventoryGrid grid)
        {
            if (grid == null || grid.GetInventory() == null) return null;
            try
            {
                object element = Traverse.Create(grid).Method("GetHoveredElement").GetValue();
                if (element == null)
                {
                    var elements = Traverse.Create(grid).Field("m_elements").GetValue() as IEnumerable;
                    if (elements != null)
                    {
                        foreach (object e in elements)
                        {
                            var go = Traverse.Create(e).Field("m_go").GetValue<GameObject>();
                            if (go != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)go.transform, Input.mousePosition))
                            {
                                element = e;
                                break;
                            }
                        }
                    }
                }
                if (element == null) return null;
                Vector2i pos = Traverse.Create(element).Field("m_pos").GetValue<Vector2i>();
                return grid.GetInventory().GetItemAt(pos.x, pos.y);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Objet survolé introuvable : " + e.Message);
                return null;
            }
        }

        public static void Open(ItemDrop.ItemData item)
        {
            Close();
            ChestPanel.Close();
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
