using System.Collections.Generic;
using HarmonyLib;

namespace SmartChest.Chests
{
    /// <summary>
    /// Liste des coffres chargés, et lien inventaire → coffre
    /// (Quick Stack Store ne nous donne que l'inventaire d'un coffre).
    /// </summary>
    public static class ContainerRegistry
    {
        private static readonly HashSet<Container> _all = new HashSet<Container>();
        private static readonly Dictionary<Inventory, Container> _byInventory = new Dictionary<Inventory, Container>();

        public static List<Container> All()
        {
            // Les coffres détruits deviennent "null" pour Unity : on les retire au passage
            _all.RemoveWhere(c => c == null);
            var dead = new List<Inventory>();
            foreach (var kv in _byInventory) if (kv.Value == null) dead.Add(kv.Key);
            foreach (Inventory inv in dead) _byInventory.Remove(inv);
            return new List<Container>(_all);
        }

        public static Container FromInventory(Inventory inv)
        {
            if (inv != null && _byInventory.TryGetValue(inv, out Container c))
            {
                if (c != null) return c;
                _byInventory.Remove(inv);
            }
            return null;
        }

        internal static void Add(Container c)
        {
            if (c == null) return;
            _all.Add(c);
            // Étiquette flottante (elle reste invisible tant que le coffre n'a pas de catégorie)
            if (c.GetComponent<SmartChest.UI.ChestLabel>() == null) c.gameObject.AddComponent<SmartChest.UI.ChestLabel>();
            Inventory inv = c.GetInventory();
            if (inv != null) _byInventory[inv] = c;
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        private static class AwakePatch
        {
            private static void Postfix(Container __instance) => Add(__instance);
        }

    }
}
