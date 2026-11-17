using System;
using System.Collections.Generic;

namespace SmartChest.Catalog
{
    /// <summary>
    /// Catégories par défaut et règles qui attribuent une catégorie à chaque objet du jeu
    /// (y compris ceux d'autres mods) à partir de son type interne et de son nom de prefab.
    /// Les types sont comparés par leur nom (texte) pour rester compatibles entre versions du jeu.
    /// </summary>
    public static class DefaultRules
    {
        public const string WoodStone = "bois_pierre";
        public const string OreMetal = "minerais";
        public const string Materials = "materiaux";
        public const string Food = "nourriture";
        public const string RawIngredients = "ingredients";
        public const string Meads = "hydromels";
        public const string Seeds = "graines";
        public const string Weapons = "armes";
        public const string Armor = "armures";
        public const string Ammo = "munitions";
        public const string Tools = "outils";
        public const string Trophies = "trophees";
        public const string Fish = "poissons";
        public const string BossItems = "boss";
        public const string Misc = "divers";

        public static List<Category> CreateDefaultCategories()
        {
            return new List<Category>
            {
                Def(WoodStone, "A0703C"),
                Def(OreMetal, "E8A33D"),
                Def(Materials, "B5B5B5"),
                Def(Food, "7BC043"),
                Def(RawIngredients, "D9534F"),
                Def(Meads, "9B59B6"),
                Def(Seeds, "4CAF50"),
                Def(Weapons, "E74C3C"),
                Def(Armor, "3498DB"),
                Def(Ammo, "F1C40F"),
                Def(Tools, "95A5A6"),
                Def(Trophies, "FFD700"),
                Def(Fish, "1ABC9C"),
                Def(BossItems, "C0392B"),
                Def(Misc, "7F8C8D"),
            };
        }

        private static Category Def(string id, string color) =>
            new Category { Id = id, Name = "$smartchest_cat_" + id, Color = color, IsDefault = true };

        private static readonly HashSet<string> WoodStoneItems = Set(
            "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood",
            "Stone", "Flint", "Grausten", "BlackMarble", "Obsidian", "Coal", "Resin", "Tar");

        private static readonly HashSet<string> MetalItems = Set(
            "Copper", "Tin", "Bronze", "Iron", "Silver", "BlackMetal", "Flametal", "FlametalNew",
            "Ironpit", "IronNails", "BronzeNails", "Eitr");

        private static readonly HashSet<string> RawIngredientItems = Set(
            "RawMeat", "DeerMeat", "NeckTail", "FishRaw", "WolfMeat", "LoxMeat", "SerpentMeat",
            "ChickenMeat", "HareMeat", "BugMeat", "AsksvinMeat", "VoltureMeat", "BoneMawSerpentMeat",
            "Entrails", "Bloodbag", "Barley", "BarleyFlour", "Flax", "Ooze", "Thistle", "Dandelion",
            "Sap", "RoyalJelly", "ChickenEgg", "Fiddleheadfern", "Vineberry", "MushroomSmokePuff",
            "Carrot", "Turnip", "Onion", "MushroomYellow", "MushroomBlue", "MushroomJotunPuffs",
            "MushroomMagecap", "Honey", "Raspberry", "Blueberries", "Cloudberry");

        private static readonly HashSet<string> SeedItems = Set(
            "CarrotSeeds", "TurnipSeeds", "OnionSeeds", "BeechSeeds", "BirchSeeds", "Acorn",
            "PineCone", "FirCone", "SeedCarrot", "SeedTurnip", "SeedOnion", "VineberrySeeds");

        private static readonly HashSet<string> BossItemSet = Set(
            "HardAntler", "CryptKey", "Wishbone", "DragonTear", "YagluthDrop", "QueenDrop",
            "FaderDrop", "DragonEgg", "AncientSeed", "WitheredBone", "GoblinTotem", "DvergrKey",
            "BellFragment", "Bell", "SummonFader", "BonemawSerpentScale");

        /// <summary>Calcule les catégories par défaut d'un objet. Ne renvoie jamais une liste vide.</summary>
        public static List<string> Compute(ItemDrop.ItemData item)
        {
            var result = new List<string>();
            if (item?.m_shared == null)
            {
                result.Add(Misc);
                return result;
            }

            ItemDrop.ItemData.SharedData shared = item.m_shared;
            string prefab = CatalogManager.GetPrefabName(item);
            string type = shared.m_itemType.ToString();
            string skill = shared.m_skillType.ToString();

            bool isFood = shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f;

            // Listes explicites d'abord (plus précises que le type)
            if (BossItemSet.Contains(prefab)) result.Add(BossItems);
            if (WoodStoneItems.Contains(prefab)) result.Add(WoodStone);
            if (MetalItems.Contains(prefab) || prefab.EndsWith("Ore") || prefab.EndsWith("Scrap")) result.Add(OreMetal);
            if (SeedItems.Contains(prefab) || prefab.EndsWith("Seeds") || prefab.StartsWith("Sapling")) result.Add(Seeds);
            if (RawIngredientItems.Contains(prefab)) result.Add(RawIngredients);

            switch (type)
            {
                case "Trophy":
                    result.Add(Trophies);
                    break;
                case "Fish":
                    result.Add(Fish);
                    break;
                case "Ammo":
                case "AmmoNonEquipable":
                    result.Add(Ammo);
                    break;
                case "Helmet":
                case "Chest":
                case "Legs":
                case "Shoulder":
                case "Hands":
                case "Utility":
                case "Trinket":
                    result.Add(Armor);
                    break;
                case "Tool":
                case "Torch":
                    result.Add(Tools);
                    break;
                case "OneHandedWeapon":
                case "TwoHandedWeapon":
                case "TwoHandedWeaponLeft":
                case "Bow":
                case "Shield":
                case "Attach_Atgeir":
                    // Pioches, haches de bûcheron et canne à pêche : outils ; le reste : armes
                    if (skill == "Pickaxes" || skill == "Fishing" || prefab.StartsWith("Pickaxe"))
                        result.Add(Tools);
                    else
                        result.Add(Weapons);
                    break;
                case "Consumable":
                    if (prefab.StartsWith("Mead") || prefab.StartsWith("BarleyWine") || (!isFood && shared.m_consumeStatusEffect != null))
                        result.Add(Meads);
                    else if (isFood)
                        result.Add(Food);
                    break;
                case "Material":
                    // Poissons attrapés (Fish1, Fish2...) et appâts (FishingBait...)
                    if (prefab.StartsWith("Fish") && prefab != "FishRaw" && prefab != "FishCooked")
                        result.Add(Fish);
                    else if (result.Count == 0)
                        result.Add(Materials);
                    break;
            }

            if (isFood && !result.Contains(Food) && !result.Contains(Meads)) result.Add(Food);
            if (result.Count == 0) result.Add(Misc);
            return result;
        }

        private static HashSet<string> Set(params string[] items) =>
            new HashSet<string>(items, StringComparer.Ordinal);
    }
}
