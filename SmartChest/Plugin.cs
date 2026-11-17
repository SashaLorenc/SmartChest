using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SmartChest.Catalog;
using SmartChest.Storing;
using SmartChest.UI;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace SmartChest
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    // Quick Stack Store Sort Trash Restock : dépendance optionnelle (on se branche seulement s'il est là)
    [BepInDependency(QuickStackStoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "sashalorenc.SmartChest";
        public const string ModName = "Smart Chest";
        public const string ModVersion = "0.1.0";

        public const string QuickStackStoreGuid = "goldenrevolver.quick_stack_store";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public static ConfigEntry<float> StoreRadius;
        public static ConfigEntry<KeyboardShortcut> StoreKey;
        public static ConfigEntry<KeyboardShortcut> EditItemKey;
        public static ConfigEntry<bool> SkipHotbar;
        public static ConfigEntry<bool> ShowCategoriesInTooltip;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            BindConfig();
            CatalogManager.Load();
            CommandManager.Instance.AddConsoleCommand(new DumpCommand());

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            bool qss = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(QuickStackStoreGuid);
            Logger.LogInfo($"{ModName} {ModVersion} chargé. Quick Stack Store détecté : {qss}");
        }

        private void Update()
        {
            if (Player.m_localPlayer == null || !StoreKey.Value.IsDown()) return;
            if (IsTypingSomewhere()) return;
            StoreService.StoreNearby(Player.m_localPlayer);
        }

        /// <summary>Le joueur écrit dans le chat, la console, un panneau... : on ignore la touche.</summary>
        private static bool IsTypingSomewhere()
        {
            if (ChestPanel.IsTyping) return true;
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            if (Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible()) return true;
            if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large) return true;
            return false;
        }

        private void BindConfig()
        {
            StoreRadius = Config.Bind("Rangement", "Rayon", 10f,
                new ConfigDescription("Distance maximale (m) des coffres utilisés pour le rangement.",
                    new AcceptableValueRange<float>(1f, 50f)));

            StoreKey = Config.Bind("Touches", "RangerParCategorie", new KeyboardShortcut(KeyCode.L),
                "Range l'inventaire dans les coffres catégorisés à portée.");

            EditItemKey = Config.Bind("Touches", "EditerObjet", new KeyboardShortcut(KeyCode.K),
                "Sur un objet survolé dans l'inventaire : modifier ses catégories.");

            ShowCategoriesInTooltip = Config.Bind("Affichage", "CategoriesDansInfobulle", true,
                "Affiche les catégories d'un objet dans son infobulle.");

            SkipHotbar = Config.Bind("Rangement", "IgnorerBarreRaccourcis", true,
                "Ne jamais ranger les objets de la barre de raccourcis (1 à 8).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
