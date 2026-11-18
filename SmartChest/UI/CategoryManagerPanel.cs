using System.Collections.Generic;
using Jotunn.Managers;
using SmartChest.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace SmartChest.UI
{
    /// <summary>
    /// Gestion des catégories : renommer, changer de couleur, masquer (catégories par défaut)
    /// ou supprimer (catégories perso). Ouvert depuis le panneau d'un coffre (bouton « Gérer »).
    /// </summary>
    public static class CategoryManagerPanel
    {
        private const float RowHeight = 38f;
        private const float ColWidth = 380f;
        private const float PanelWidth = 800f;

        private static GameObject _panel;
        private static Container _returnTo;
        private static string _pendingDelete;

        public static bool IsOpen => _panel != null;

        public static void Open(Container returnTo)
        {
            Close();
            ChestPanel.Close();
            ItemEditorPanel.Close();
            _returnTo = returnTo;

            var all = new List<Category>(CatalogManager.Categories);
            int rows = (all.Count + 1) / 2;
            float height = 80f + rows * RowHeight + 70f;
            _panel = UiKit.CreatePanel(PanelWidth, height);

            UiKit.AddText(_panel.transform, Localization.instance.Localize("$smartchest_manage_title"),
                20f, 15f, PanelWidth - 40f, 30f, 22, GUIManager.Instance.ValheimOrange);

            for (int i = 0; i < all.Count; i++)
            {
                Category cat = all[i];
                string id = cat.Id;
                float x = 20f + (i % 2) * ColWidth;
                float y = 60f + (i / 2) * RowHeight;

                // Pastille de couleur : un clic passe à la couleur suivante
                GameObject colorBtn = UiKit.AddButton(_panel.transform, "", x, y, 30f, 30f, () =>
                {
                    Category c = CatalogManager.GetCategory(id);
                    if (c == null) return;
                    CatalogManager.UpdateCategory(id, null, UiKit.NextColor(c.Color));
                    Refresh();
                });
                Image img = colorBtn.GetComponent<Image>();
                if (img != null && ColorUtility.TryParseHtmlString("#" + cat.Color, out Color col)) img.color = col;

                // Nom modifiable : validé quand on quitte le champ ou appuie sur Entrée
                InputField field = UiKit.AddInput(_panel.transform, cat.DisplayName, "", x + 36f, y, 210f, 30f);
                string original = cat.DisplayName;
                field.onEndEdit.AddListener(text =>
                {
                    string name = (text ?? "").Trim();
                    if (name.Length == 0 || name == original) return;
                    CatalogManager.UpdateCategory(id, name, null);
                });
                if (cat.Hidden)
                {
                    Text t = field.textComponent;
                    if (t != null) t.color = new Color(0.6f, 0.6f, 0.6f);
                }

                // Masquer / afficher (par défaut) ou supprimer en deux clics (perso)
                string actionLabel;
                if (cat.IsDefault) actionLabel = cat.Hidden ? "$smartchest_show" : "$smartchest_hide";
                else actionLabel = _pendingDelete == id ? "$smartchest_confirm" : "$smartchest_delete";

                UiKit.AddButton(_panel.transform, actionLabel, x + 252f, y, 110f, 30f, () =>
                {
                    Category c = CatalogManager.GetCategory(id);
                    if (c == null) return;
                    if (c.IsDefault)
                    {
                        CatalogManager.SetHidden(id, !c.Hidden);
                    }
                    else if (_pendingDelete == id)
                    {
                        CatalogManager.RemoveCategory(id);
                        _pendingDelete = null;
                    }
                    else
                    {
                        _pendingDelete = id;
                    }
                    Refresh();
                });
            }

            float bottom = 60f + rows * RowHeight + 15f;
            UiKit.AddButton(_panel.transform, "$smartchest_back", PanelWidth - 20f - 150f, bottom, 150f, 34f, Back);
        }

        private static void Refresh()
        {
            Container back = _returnTo;
            string pending = _pendingDelete;
            Open(back);
            _pendingDelete = pending;
        }

        private static void Back()
        {
            Container back = _returnTo;
            _pendingDelete = null;
            Close();
            if (back != null && InventoryGui.instance != null && InventoryGui.instance.m_currentContainer == back)
                ChestPanel.Open(back);
        }

        public static void Close()
        {
            if (_panel != null) Object.Destroy(_panel);
            _panel = null;
            _returnTo = null;
        }
    }
}
