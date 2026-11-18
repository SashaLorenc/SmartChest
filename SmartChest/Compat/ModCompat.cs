using System;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SmartChest.Compat
{
    /// <summary>
    /// Détection des autres mods et règles communes. Aucun de ces mods n'est référencé à la compilation :
    /// tout passe par réflexion, et chaque appel est protégé pour qu'une mise à jour d'un autre mod
    /// ne casse pas Smart Chest (au pire, la règle prudente s'applique).
    /// Les GUID et noms de champs viennent du code source de Quick Stack Store Sort Trash Restock,
    /// qui gère déjà ces mods.
    /// </summary>
    public static class ModCompat
    {
        // Inventaires étendus / emplacements d'équipement et raccourcis
        public const string AedenEPI = "aedenthorn.ExtendedPlayerInventory";
        public const string ComfyQuickSlots = "com.bruce.valheim.comfyquickslots";
        public const string OdinsQoL = "com.odinplusqol.mod";
        public const string OdinsExtendedInventory = "odinplusqol.OdinsExtendedInventory";
        public const string RandyEquipmentQuickSlots = "randyknapp.mods.equipmentandquickslots";
        public const string AzuEPI = "Azumatt.AzuExtendedPlayerInventory";
        public const string BetterArchery = "ishid4.mods.betterarchery";

        // Coffres et stockage
        public const string MultiUserChest = "com.maxsch.valheim.MultiUserChest";
        public const string DynamicStoragePiles = "com.maxsch.valheim.DynamicStoragePiles";
        public const string AzuAutoStore = "Azumatt.AzuAutoStore";

        // Inventaires « portables » (pas des coffres posés dans le monde)
        public const string AdventureBackpacks = "vapok.mods.adventurebackpacks";
        public const string Backpacks = "org.bepinex.plugins.backpacks";
        public const string Jewelcrafting = "org.bepinex.plugins.jewelcrafting";
        public const string CurrencyPocket = "Azumatt.CurrencyPocket";
        public const string SmartContainers = "flueno.SmartContainers";

        // Interface
        public const string Auga = "randyknapp.mods.auga";

        public static bool Has(string guid) => Chainloader.PluginInfos.ContainsKey(guid);

        public static void LogDetected()
        {
            string[] all =
            {
                Plugin.QuickStackStoreGuid, AedenEPI, ComfyQuickSlots, OdinsQoL, OdinsExtendedInventory, RandyEquipmentQuickSlots,
                AzuEPI, BetterArchery, MultiUserChest, DynamicStoragePiles, AzuAutoStore, AdventureBackpacks, Backpacks,
                Jewelcrafting, CurrencyPocket, SmartContainers, Auga,
            };
            string found = string.Join(", ", all.Where(Has));
            Plugin.Log.LogInfo("Mods compatibles détectés : " + (found.Length > 0 ? found : "aucun"));
            if (Has(Auga))
                Plugin.Log.LogWarning("Project Auga remplace l'interface de l'inventaire : les boutons de Smart Chest peuvent ne pas apparaître. La touche de rangement fonctionne quand même.");
        }

        // ---------------------------------------------------------------------
        // Règles sur les coffres
        // ---------------------------------------------------------------------

        /// <summary>
        /// Vrai si le coffre est un vrai coffre posé dans le monde par un joueur.
        /// Exclut sacs à dos (Adventure Backpacks, Backpacks), sac de Jewelcrafting, Currency Pocket,
        /// tombes, coffres de donjon et tas de stockage limités à un seul objet (Dynamic Storage Piles).
        /// </summary>
        public static bool IsEligibleChest(Container c)
        {
            if (c == null || c.m_nview == null || !c.m_nview.IsValid()) return false;
            if (c.GetComponent<TombStone>() != null) return false;
            if (c.GetComponentInParent<Player>() != null) return false;
            // Le coffre d'un bateau ou d'un chariot est sur un objet enfant : on cherche la pièce construite au-dessus
            Piece piece = c.m_piece != null ? c.m_piece : c.GetComponentInParent<Piece>();
            if (piece == null || !piece.IsPlacedByPlayer()) return false;
            if (IsRestrictedPile(c)) return false;
            return true;
        }

        public static bool IsTrueSingleplayer()
        {
            return !ZNet.m_openServer && !ZNet.m_publicServer
                   && ZNet.instance != null && ZNet.instance.IsServer() && !ZNet.instance.IsDedicated()
                   && ZNet.instance.GetConnectedPeers().Count == 0;
        }

        /// <summary>Le coffre est-il utilisé par quelqu'un (vu par nous ou annoncé par le serveur) ?</summary>
        public static bool IsInUse(Container c)
        {
            if (c.IsInUse()) return true;
            if (c.m_wagon != null && c.m_wagon.InUse()) return true;
            ZDO zdo = c.m_nview.GetZDO();
            return zdo != null && zdo.GetInt("InUse", 0) == 1;
        }

        /// <summary>Peut-on modifier ce coffre à distance (touche de rangement) ? Mêmes règles que Quick Stack Store.</summary>
        public static bool CanAffectRemotely(Container c, Player player)
        {
            if (!IsEligibleChest(c)) return false;
            if (!c.CheckAccess(player.GetPlayerID())) return false;
            if (c.m_checkGuardStone && !PrivateArea.CheckAccess(c.transform.position, 0f, false, false)) return false;

            // Avec Multi User Chest, plusieurs joueurs peuvent utiliser un coffre en même temps
            if (Has(MultiUserChest)) return true;

            if (!IsTrueSingleplayer() && c.transform.root.GetComponentInChildren<Ship>() != null) return false; // trop risqué en multi
            return !IsInUse(c);
        }

        /// <summary>Prépare un coffre avant de le modifier à distance (propriété + « en cours d'utilisation »).</summary>
        public static void BeginRemoteEdit(Container c)
        {
            if (Has(MultiUserChest)) return;
            c.m_nview.ClaimOwnership();
            SetInUse(c, true);
        }

        public static void EndRemoteEdit(Container c)
        {
            if (Has(MultiUserChest)) return;
            SetInUse(c, false);
        }

        private static void SetInUse(Container c, bool inUse)
        {
            c.m_inUse = inUse;
            ZDO zdo = c.m_nview.GetZDO();
            if (zdo == null) return;
            zdo.Set("InUse", inUse ? 1 : 0);
            ZDOMan.instance.ForceSendZDO(ZNet.GetUID(), zdo.m_uid);
        }

        // ---------------------------------------------------------------------
        // Dynamic Storage Piles : tas qui n'acceptent qu'un seul type d'objet
        // ---------------------------------------------------------------------

        private static MethodInfo _isRestrictedContainer;
        private static bool _restrictedLookupFailed;

        public static bool IsRestrictedPile(Container c)
        {
            if (!Has(DynamicStoragePiles) || _restrictedLookupFailed) return false;
            try
            {
                if (_isRestrictedContainer == null)
                {
                    Type t = AccessTools.TypeByName("DynamicStoragePiles.RestrictContainers") ?? FindType("DynamicStoragePiles", "RestrictContainers");
                    _isRestrictedContainer = t?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name.Equals("IsRestrictedContainer", StringComparison.OrdinalIgnoreCase));
                    if (_isRestrictedContainer == null) { _restrictedLookupFailed = true; return false; }
                }
                object[] args = { c.m_name, null };
                return _isRestrictedContainer.Invoke(null, args) is bool b && b;
            }
            catch (Exception e)
            {
                _restrictedLookupFailed = true;
                Plugin.Log.LogWarning("Dynamic Storage Piles : détection impossible (" + e.Message + ")");
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // Emplacements d'équipement / raccourcis ajoutés par d'autres mods
        // ---------------------------------------------------------------------

        private static MethodInfo _qssIsEquipOrQuickSlot;
        private static bool _qssLookupDone;

        /// <summary>
        /// Vrai si la case appartient à un emplacement spécial (équipement, raccourcis, carquois)
        /// ajouté par un mod d'inventaire : les objets qui y sont ne doivent jamais être rangés.
        /// </summary>
        public static bool IsProtectedSlot(Inventory inv, Vector2i pos)
        {
            int h = inv.GetHeight(), w = inv.GetWidth();

            // Si Quick Stack Store est là, on réutilise directement sa fonction (toujours à jour)
            if (!_qssLookupDone)
            {
                _qssLookupDone = true;
                if (Has(Plugin.QuickStackStoreGuid))
                {
                    Type t = AccessTools.TypeByName("QuickStackStore.CompatibilitySupport");
                    _qssIsEquipOrQuickSlot = t != null ? AccessTools.Method(t, "IsEquipOrQuickSlot", new[] { typeof(int), typeof(int), typeof(Vector2i) }) : null;
                }
            }
            if (_qssIsEquipOrQuickSlot != null)
            {
                try { return (bool)_qssIsEquipOrQuickSlot.Invoke(null, new object[] { h, w, pos }); }
                catch { /* on retombe sur nos propres règles */ }
            }

            return IsAedenLikeSlot(h, pos) || IsAzuEpiSlot(h, w, pos) || IsComfySlot(pos) || IsBetterArcherySlot(pos);
        }

        // --- Aedenthorn Extended Player Inventory, Odin's Extended Inventory, Odin's QoL, ancienne Azu EPI :
        //     une rangée d'équipement en bas de l'inventaire.
        private static bool IsAedenLikeSlot(int height, Vector2i pos)
        {
            if (pos.y != height - 1) return false;
            return (Has(AedenEPI) && BoolField("ExtendedPlayerInventory", "BepInExPlugin", "addEquipmentRow"))
                   || (Has(OdinsExtendedInventory) && BoolField("OdinsExtendedInventory", "OdinsExtendedInventoryPlugin", "addEquipmentRow"))
                   || (Has(OdinsQoL) && BoolField("OdinQOL", "QuickAccessBar", "AddEquipmentRow"));
        }

        // --- Azu Extended Player Inventory (version avec API) : rangées ajoutées en bas.
        private static MethodInfo _azuGetAddedRows;
        private static bool _azuFailed;

        private static bool IsAzuEpiSlot(int height, int width, Vector2i pos)
        {
            if (!Has(AzuEPI) || _azuFailed) return false;
            try
            {
                if (_azuGetAddedRows == null)
                {
                    Type api = AccessTools.TypeByName("AzuExtendedPlayerInventory.API");
                    _azuGetAddedRows = api != null ? AccessTools.Method(api, "GetAddedRows", new[] { typeof(int) }) : null;
                }
                int rows;
                if (_azuGetAddedRows != null)
                    rows = (int)_azuGetAddedRows.Invoke(null, new object[] { width });
                else
                    rows = ToggleField("AzuExtendedPlayerInventory", "AzuExtendedPlayerInventoryPlugin", "AddEquipmentRow") ? 1 : 0;
                return rows > 0 && pos.y >= height - rows;
            }
            catch
            {
                _azuFailed = true;
                return pos.y == height - 1; // prudence : on protège la dernière rangée
            }
        }

        // --- Comfy Quick Slots
        private static MethodInfo _comfyIsArmorSlot;
        private static bool _comfyFailed;

        private static bool IsComfySlot(Vector2i pos)
        {
            if (!Has(ComfyQuickSlots) || _comfyFailed) return false;
            try
            {
                if (_comfyIsArmorSlot == null)
                {
                    Type t = FindType("ComfyQuickSlots", "QuickSlotsManager");
                    _comfyIsArmorSlot = t?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name.Equals("IsArmorSlot", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1);
                    if (_comfyIsArmorSlot == null) { _comfyFailed = true; return false; }
                }
                // Emplacement d'armure, ou emplacement de raccourci (3 cases à droite des armures)
                return (bool)_comfyIsArmorSlot.Invoke(null, new object[] { pos })
                       || (bool)_comfyIsArmorSlot.Invoke(null, new object[] { new Vector2i(pos.x - 3, pos.y) });
            }
            catch
            {
                _comfyFailed = true;
                return false;
            }
        }

        // --- Better Archery : rangées de carquois
        private static FieldInfo _baEnabled, _baRow;
        private static bool _baFailed;

        private static bool IsBetterArcherySlot(Vector2i pos)
        {
            if (!Has(BetterArchery) || _baFailed) return false;
            try
            {
                if (_baEnabled == null || _baRow == null)
                {
                    Type t = FindType("BetterArchery", "BetterArchery");
                    FieldInfo[] fields = t?.GetFields(BindingFlags.Public | BindingFlags.Static) ?? new FieldInfo[0];
                    _baEnabled = fields.FirstOrDefault(f => f.Name.Equals("ConfigQuiverEnabled", StringComparison.OrdinalIgnoreCase));
                    _baRow = fields.FirstOrDefault(f => f.Name.Equals("QuiverRowIndex", StringComparison.OrdinalIgnoreCase));
                    if (_baEnabled == null || _baRow == null) { _baFailed = true; return false; }
                }
                if (!(_baEnabled.GetValue(null) is ConfigEntry<bool> on) || !on.Value) return false;
                if (!(_baRow.GetValue(null) is int row) || row == 0) return false;
                return pos.y == row || pos.y == row - 1;
            }
            catch
            {
                _baFailed = true;
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // Réflexion
        // ---------------------------------------------------------------------

        private static Type FindType(string assemblyName, string className)
        {
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
            return asm?.GetTypes().FirstOrDefault(t => t.IsClass && t.Name == className);
        }

        private static bool BoolField(string assemblyName, string className, string fieldName)
        {
            try
            {
                FieldInfo f = FindType(assemblyName, className)?.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                return f?.GetValue(null) is ConfigEntry<bool> c && c.Value;
            }
            catch
            {
                return true; // prudence : on protège la rangée
            }
        }

        /// <summary>Champ de config de type « Toggle » (enum On/Off) : vrai si différent de 0.</summary>
        private static bool ToggleField(string assemblyName, string className, string fieldName)
        {
            try
            {
                FieldInfo f = FindType(assemblyName, className)?.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                object v = f?.GetValue(null);
                if (v is ConfigEntry<bool> b) return b.Value;
                if (v is ConfigEntryBase e) return Convert.ToInt32(e.BoxedValue) != 0;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
