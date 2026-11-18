using SmartChest.Storing;
using UnityEngine;

namespace SmartChest.UI
{
    /// <summary>
    /// Boutons sur l'inventaire du joueur, comme ceux de Quick Stack Store :
    /// « Ranger par catégorie » (même effet que la touche L) et « Rechercher ».
    /// Ils sont enfants du panneau d'inventaire du joueur, donc visibles seulement quand l'inventaire est ouvert.
    /// </summary>
    public static class InventoryButtons
    {
        private static GameObject _bar;

        internal static void OnInventoryUpdate(InventoryGui gui)
        {
            if (_bar != null || gui.m_player == null) return;
            _bar = UiKit.CreateButtonBar(gui.m_player, "SmartChestInventoryButtons", Plugin.InventoryButtonsPlacement.Value,
                new Vector2(Plugin.InventoryButtonsOffsetX.Value, Plugin.InventoryButtonsOffsetY.Value), Plugin.ButtonsWidth.Value + 30f,
                new[] { "$smartchest_store_by_category", "$smartchest_search" },
                new UnityEngine.Events.UnityAction[]
                {
                    () => StoreService.StoreNearby(Player.m_localPlayer),
                    () =>
                    {
                        if (SearchPanel.IsOpen) SearchPanel.Close();
                        else SearchPanel.Open();
                    },
                });
        }

        /// <summary>Recrée les boutons au prochain affichage (après un changement de config).</summary>
        public static void Reset()
        {
            if (_bar != null) Object.Destroy(_bar);
            _bar = null;
        }
    }
}
