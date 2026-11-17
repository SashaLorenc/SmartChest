using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Jotunn.Entities;
using UnityEngine;

namespace SmartChest.Catalog
{
    /// <summary>
    /// Commande console (F5) « smartchest_dump » : écrit les catégories de tous les objets du jeu
    /// dans BepInEx/config/SmartChest/items_dump.txt, pour vérifier les règles par défaut.
    /// </summary>
    internal class DumpCommand : ConsoleCommand
    {
        public override string Name => "smartchest_dump";
        public override string Help => "Écrit la catégorie de chaque objet dans BepInEx/config/SmartChest/items_dump.txt";

        public override void Run(string[] args)
        {
            if (ObjectDB.instance == null)
            {
                Console.instance.Print("ObjectDB pas encore chargé : lance un monde d'abord.");
                return;
            }

            // Pas de tuples C# ici : System.ValueTuple n'existe pas dans le Mono de Valheim.
            var lines = new List<string>();
            foreach (GameObject go in ObjectDB.instance.m_items)
            {
                if (go == null) continue;
                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData?.m_shared == null) continue;
                // Attaques de monstres (pas d'icône) : jamais dans un inventaire, on les ignore
                if (drop.m_itemData.m_shared.m_icons == null || drop.m_itemData.m_shared.m_icons.Length == 0) continue;

                ItemDrop.ItemData data = drop.m_itemData.Clone();
                data.m_dropPrefab = go;
                List<string> cats = CatalogManager.GetItemCategories(data);
                string name = Localization.instance.Localize(data.m_shared.m_name);
                lines.Add($"{string.Join(",", cats),-28}\t{go.name,-32}\t{data.m_shared.m_itemType,-18}\t{name}");
            }
            lines.Sort(System.StringComparer.Ordinal);

            string path = Path.Combine(Paths.ConfigPath, "SmartChest", "items_dump.txt");
            File.WriteAllText(path, string.Join("\n", lines), Encoding.UTF8);
            Console.instance.Print($"{lines.Count} objets écrits dans {path}");
        }
    }
}
