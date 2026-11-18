using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SmartChest.Catalog;
using SmartChest.Compat;
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
    // Autres mods pris en charge (optionnels) : on se charge après eux pour pouvoir les détecter
    [BepInDependency(ModCompat.AzuAutoStore, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModCompat.AzuEPI, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModCompat.MultiUserChest, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModCompat.DynamicStoragePiles, BepInDependency.DependencyFlags.SoftDependency)]
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
        public static ConfigEntry<bool> QuickStackIntegration;
        public static ConfigEntry<bool> AutoStoreIntegration;
        public static ConfigEntry<ButtonPlacement> ButtonsPlacement;
        public static ConfigEntry<float> ButtonsOffsetX;
        public static ConfigEntry<float> ButtonsOffsetY;
        public static ConfigEntry<float> ButtonsWidth;
        public static ConfigEntry<ButtonPlacement> InventoryButtonsPlacement;
        public static ConfigEntry<float> InventoryButtonsOffsetX;
        public static ConfigEntry<float> InventoryButtonsOffsetY;
        public static ConfigEntry<float> SearchRadius;
        public static ConfigEntry<float> SearchDuration;
        public static ConfigEntry<bool> FullChestAlert;

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
            HoverTracker.Init(_harmony);
            QuickStackStoreCompat.TryInit(_harmony);
            AzuAutoStoreCompat.TryInit(_harmony);

            Logger.LogInfo($"{ModName} {ModVersion} chargé.");
        }

        private void Start()
        {
            // Tous les plugins sont chargés à ce moment-là
            ModCompat.LogDetected();
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

            ButtonsPlacement = Config.Bind("Interface", "PositionBoutons", ButtonPlacement.Dessous,
                "Où placer les boutons Catégories / Ranger ici autour du coffre ouvert : Dessus, Dessous, Gauche, Droite.");
            ButtonsOffsetX = Config.Bind("Interface", "DecalageX", 0f,
                "Décalage horizontal des boutons en pixels (positif = vers la droite).");
            ButtonsOffsetY = Config.Bind("Interface", "DecalageY", 0f,
                "Décalage vertical des boutons en pixels (positif = vers le haut).");
            ButtonsWidth = Config.Bind("Interface", "LargeurBoutons", 150f,
                new ConfigDescription("Largeur de chaque bouton en pixels.", new AcceptableValueRange<float>(80f, 300f)));
            ButtonsPlacement.SettingChanged += (_, __) => ChestPanel.ResetButtons();
            ButtonsOffsetX.SettingChanged += (_, __) => ChestPanel.ResetButtons();
            ButtonsOffsetY.SettingChanged += (_, __) => ChestPanel.ResetButtons();
            ButtonsWidth.SettingChanged += (_, __) => { ChestPanel.ResetButtons(); InventoryButtons.Reset(); };

            InventoryButtonsPlacement = Config.Bind("Interface", "PositionBoutonsInventaire", ButtonPlacement.Dessus,
                "Où placer les boutons Ranger par catégorie / Rechercher autour de l'inventaire du joueur : Dessus, Dessous, Gauche, Droite.");
            InventoryButtonsOffsetX = Config.Bind("Interface", "InventaireDecalageX", 0f,
                "Décalage horizontal des boutons de l'inventaire en pixels (positif = vers la droite).");
            InventoryButtonsOffsetY = Config.Bind("Interface", "InventaireDecalageY", 0f,
                "Décalage vertical des boutons de l'inventaire en pixels (positif = vers le haut).");
            InventoryButtonsPlacement.SettingChanged += (_, __) => InventoryButtons.Reset();
            InventoryButtonsOffsetX.SettingChanged += (_, __) => InventoryButtons.Reset();
            InventoryButtonsOffsetY.SettingChanged += (_, __) => InventoryButtons.Reset();

            SearchRadius = Config.Bind("Recherche", "Rayon", 30f,
                new ConfigDescription("Distance maximale (m) des coffres fouillés par la recherche.", new AcceptableValueRange<float>(5f, 100f)));
            SearchDuration = Config.Bind("Recherche", "Duree", 15f,
                new ConfigDescription("Durée (secondes) pendant laquelle les coffres trouvés brillent.", new AcceptableValueRange<float>(3f, 60f)));

            FullChestAlert = Config.Bind("Rangement", "AlerteCoffrePlein", true,
                "Prévient (en haut à gauche) quand un coffre catégorisé est plein et que des objets de sa catégorie n'ont pas pu être rangés.");

            QuickStackIntegration = Config.Bind("Compatibilite", "QuickStackStore", true,
                "Avec Quick Stack Store Sort Trash Restock : le stockage rapide envoie aussi les objets vers les coffres de leur catégorie, et Tout stocker respecte les coffres en mode strict.");

            AutoStoreIntegration = Config.Bind("Compatibilite", "AzuAutoStore", true,
                "Avec AzuAutoStore : les coffres catégorisés reçoivent les objets de leurs catégories, et en mode strict refusent les autres.");

            SkipHotbar = Config.Bind("Rangement", "IgnorerBarreRaccourcis", true,
                "Ne jamais ranger les objets de la barre de raccourcis (1 à 8).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
