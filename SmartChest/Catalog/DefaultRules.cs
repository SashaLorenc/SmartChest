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
        public const string Valuables = "valeurs";
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
                Def(Valuables, "F7DC6F"),
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
            "FlametalOreNew", "IronNails", "BronzeNails", "Eitr");

        private static readonly HashSet<string> RawIngredientItems = Set(
            "RawMeat", "DeerMeat", "NeckTail", "FishRaw", "WolfMeat", "LoxMeat", "SerpentMeat",
            "ChickenMeat", "HareMeat", "BugMeat", "AsksvinMeat", "VoltureMeat", "BoneMawSerpentMeat",
            "Entrails", "Bloodbag", "Barley", "BarleyFlour", "Flax", "Ooze", "Thistle", "Dandelion",
            "Sap", "RoyalJelly", "ChickenEgg", "Fiddleheadfern", "Vineberry", "MushroomSmokePuff",
            "Carrot", "Turnip", "Onion", "MushroomYellow", "MushroomBlue", "MushroomJotunPuffs",
            "MushroomMagecap", "Honey", "Raspberry", "Blueberries", "Cloudberry", "MushroomBzerker",
            "FreshSeaweed", "CuredSquirrelHamstring", "AsksvinEgg", "VoltureEgg");

        private static readonly HashSet<string> ValuableItems = Set(
            "Coins", "Amber", "AmberPearl", "Ruby", "SilverNecklace", "CrownJewel", "AncientCoin", "Gold",
            "GemstoneBlue", "GemstoneGreen", "GemstoneRed", "AncientGemstoneBlack", "AncientGemstoneGreen",
            "AncientGemstoneOrange", "AncientGemstonePurple");

        /// <summary>Consommables à effet qui ne sont ni des hydromels ni de la nourriture.</summary>
        private static readonly HashSet<string> NotMeads = Set(
            "Pukeberries", "RottenMeat", "RottingElkMeat_TW", "MushroomBzerker");

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
            if (RawIngredientItems.Contains(prefab)
                || prefab.EndsWith("Uncooked") && !IsGearMold(prefab)
                || prefab.EndsWith("Dough") || prefab.EndsWith("Flour")
                || prefab.StartsWith("Spice") || prefab.StartsWith("Feast") && prefab.EndsWith("_Material"))
                result.Add(RawIngredients);
            // Bases à mettre dans le fermenteur
            if (prefab.StartsWith("MeadBase") || prefab.StartsWith("HoneyMeadBase") || prefab == "BarleyWineBase" || prefab.EndsWith("Meadbase"))
                result.Add(Meads);
            // Pierres précieuses (vanilla + mods qui suffixent _TW : Amethyst_TW, Diamond_TW...)
            if (ValuableItems.Contains(prefab) || IsModGem(prefab)) result.Add(Valuables);

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
                    if (NotMeads.Contains(prefab))
                    {
                        if (result.Count == 0) result.Add(Misc);
                    }
                    else if (prefab.StartsWith("Mead") || prefab.StartsWith("BarleyWine") || (!isFood && shared.m_consumeStatusEffect != null))
                        result.Add(Meads);
                    else if (isFood)
                        result.Add(Food);
                    break;
                case "Material":
                    // Appâts de pêche ; les poissons attrapés ont leur propre type "Fish"
                    if (prefab.StartsWith("FishingBait"))
                        result.Add(Fish);
                    // Viande crue, plats crus... : leurs stats de nourriture ne servent à rien, c'est un ingrédient
                    else if (isFood && !result.Contains(RawIngredients))
                        result.Add(RawIngredients);
                    else if (result.Count == 0)
                        result.Add(Materials);
                    break;
            }

            // Nourriture mangeable telle quelle (pas les matériaux crus)
            if (isFood && type != "Material" && !result.Contains(Food) && !result.Contains(Meads)) result.Add(Food);
            if (result.Count == 0) result.Add(Misc);
            return result;
        }

        /// <summary>Armes/armures « Uncooked » d'un mod de moules (ArmorGold...Uncooked) : ce ne sont pas des aliments.</summary>
        private static bool IsGearMold(string prefab) =>
            prefab.Contains("Gold") || prefab.Contains("Mold") || prefab.StartsWith("Staff") || prefab.StartsWith("SmallParts");

        private static readonly string[] GemNames =
            { "Amethyst", "Diamond", "Emerald", "Onyx", "Sapphire", "Topaz", "DarkCrystal" };

        private static bool IsModGem(string prefab)
        {
            foreach (string g in GemNames)
                if (prefab == g || prefab == g + "_TW") return true;
            return false;
        }

        private static HashSet<string> Set(params string[] items) =>
            new HashSet<string>(items, StringComparer.Ordinal);
    }
}
