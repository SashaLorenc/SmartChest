using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SmartChest.Catalog;
using SmartChest.Chests;

namespace SmartChest.Compat
{
    /// <summary>
    /// Intégration optionnelle avec Quick Stack Store Sort Trash Restock (Goldenrevolver).
    /// Ce mod n'est pas référencé à la compilation : on retrouve ses méthodes par leur nom
    /// et on les patche seulement s'il est installé. Si son code change, l'intégration
    /// se désactive avec un avertissement dans le log, sans casser le reste.
    ///
    /// - Stockage rapide (Quick Stack) : un coffre catégorisé reçoit aussi les objets de ses
    ///   catégories, même s'il n'en contient pas encore.
    /// - Tout stocker (Store All) : sur un coffre catégorisé en mode strict, seuls les objets
    ///   de ses catégories sont déposés.
    /// - Ses favoris sont respectés par notre propre rangement.
    /// </summary>
    public static class QuickStackStoreCompat
    {
        public static bool Active { get; private set; }

        private static MethodInfo _getPlayerConfig;
        private static MethodInfo _isFavorited;

        public static void TryInit(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Plugin.QuickStackStoreGuid)) return;

            try
            {
                Type quickStack = ModCompat.PluginType(Plugin.QuickStackStoreGuid, "QuickStackStore.QuickStackModule");
                Type storeAll = ModCompat.PluginType(Plugin.QuickStackStoreGuid, "QuickStackStore.StoreTakeAllModule");
                Type userConfig = ModCompat.PluginType(Plugin.QuickStackStoreGuid, "QuickStackStore.UserConfig");

                MethodInfo quickStackInto = quickStack != null ? AccessTools.Method(quickStack, "QuickStackIntoThisContainer") : null;
                MethodInfo shouldStore = storeAll != null ? AccessTools.Method(storeAll, "ShouldStoreItem") : null;

                if (quickStackInto != null)
                    harmony.Patch(quickStackInto, postfix: new HarmonyMethod(typeof(QuickStackStoreCompat), nameof(QuickStackPostfix)));
                else
                    Plugin.Log.LogWarning("Quick Stack Store : méthode QuickStackIntoThisContainer introuvable, stockage rapide non intégré.");

                if (shouldStore != null)
                    harmony.Patch(shouldStore, postfix: new HarmonyMethod(typeof(QuickStackStoreCompat), nameof(ShouldStorePostfix)));
                else
                    Plugin.Log.LogWarning("Quick Stack Store : méthode ShouldStoreItem introuvable, Tout stocker non intégré.");

                if (userConfig != null)
                {
                    _getPlayerConfig = AccessTools.Method(userConfig, "GetPlayerConfig", new[] { typeof(long) });
                    _isFavorited = AccessTools.Method(userConfig, "IsItemNameOrSlotFavorited", new[] { typeof(ItemDrop.ItemData) });
                }

                Active = quickStackInto != null || shouldStore != null;
                Plugin.Log.LogInfo($"Intégration Quick Stack Store : {(Active ? "active" : "inactive")}.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Intégration Quick Stack Store désactivée : {e.Message}");
            }
        }

        // La config du joueur est relue une fois par image (et non pour chaque objet)
        private static object _cachedConfig;
        private static long _cachedPlayerId;
        private static int _cachedFrame = -1;

        /// <summary>Vrai si l'objet est en favori dans Quick Stack Store (il ne doit jamais bouger).</summary>
        public static bool IsFavorited(Player player, ItemDrop.ItemData item)
        {
            if (_getPlayerConfig == null || _isFavorited == null || player == null) return false;
            try
            {
                long id = player.GetPlayerID();
                if (_cachedFrame != UnityEngine.Time.frameCount || _cachedPlayerId != id)
                {
                    _cachedConfig = _getPlayerConfig.Invoke(null, new object[] { id });
                    _cachedPlayerId = id;
                    _cachedFrame = UnityEngine.Time.frameCount;
                }
                return _cachedConfig != null && (bool)_isFavorited.Invoke(_cachedConfig, new object[] { item });
            }
            catch
            {
                return false;
            }
        }

        // Signature d'origine : QuickStackIntoThisContainer(List<ItemData> trophies, List<ItemData> nonTrophies,
        //     Inventory playerInventory, Inventory container, bool callPlayerInvChanged) -> int
        private static void QuickStackPostfix(List<ItemDrop.ItemData> trophies, List<ItemDrop.ItemData> nonTrophies,
            Inventory playerInventory, Inventory container, bool callPlayerInvChanged, ref int __result)
        {
            if (!Plugin.QuickStackIntegration.Value) return;

            Container chest = ContainerRegistry.FromInventory(container);
            if (chest == null) return;
            var chestCats = new HashSet<string>(ChestCategories.Get(chest));
            if (chestCats.Count == 0) return;

            int moved = MoveMatching(nonTrophies, chestCats, playerInventory, container)
                        + MoveMatching(trophies, chestCats, playerInventory, container);

            if (moved > 0)
            {
                __result += moved;
                if (callPlayerInvChanged) playerInventory.Changed();
            }
        }

        private static int MoveMatching(List<ItemDrop.ItemData> items, HashSet<string> chestCats, Inventory from, Inventory to)
        {
            if (items == null) return 0;
            int moved = 0;
            // Même logique que Quick Stack Store : on parcourt à l'envers et on retire de la liste ce qui est rangé
            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = items[i];
                if (!CatalogManager.MatchesAny(item, chestCats)) continue;

                if (to.AddItem(item))
                {
                    from.RemoveItem(item);
                    items.RemoveAt(i);
                    moved++;
                }
            }
            return moved;
        }

        // Signature d'origine : ShouldStoreItem(ItemData item, UserConfig playerConfig, int inventoryHeight,
        //     int inventoryWidth, bool includeHotbar) -> bool
        private static void ShouldStorePostfix(ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result || !Plugin.QuickStackIntegration.Value) return;

            Container chest = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;
            if (chest == null || ChestCategories.GetMode(chest) != ChestMode.Strict) return;

            List<string> chestCats = ChestCategories.Get(chest);
            if (chestCats.Count == 0) return;

            if (!CatalogManager.MatchesAny(item, chestCats))
                __result = false;
        }
    }
}
