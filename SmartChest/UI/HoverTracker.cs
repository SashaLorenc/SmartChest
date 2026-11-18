using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace SmartChest.UI
{
    /// <summary>
    /// Suit la case d'inventaire survolée grâce aux événements de l'interface du jeu
    /// (UIInputHandler implémente IPointerEnterHandler / IPointerExitHandler, donc ces méthodes existent).
    /// Patché à la main avec vérification, pour qu'un changement du jeu ne casse pas tout le mod.
    /// </summary>
    internal static class HoverTracker
    {
        internal static void Init(Harmony harmony)
        {
            try
            {
                MethodInfo enter = AccessTools.Method(typeof(UIInputHandler), nameof(IPointerEnterHandler.OnPointerEnter), new[] { typeof(PointerEventData) });
                MethodInfo exit = AccessTools.Method(typeof(UIInputHandler), nameof(IPointerExitHandler.OnPointerExit), new[] { typeof(PointerEventData) });

                if (enter != null) harmony.Patch(enter, postfix: new HarmonyMethod(typeof(HoverTracker), nameof(EnterPostfix)));
                if (exit != null) harmony.Patch(exit, postfix: new HarmonyMethod(typeof(HoverTracker), nameof(ExitPostfix)));

                if (enter == null || exit == null)
                    Plugin.Log.LogWarning("UIInputHandler.OnPointerEnter/Exit introuvable : la touche K ne pourra pas détecter l'objet survolé.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Suivi du survol désactivé : " + e.Message);
            }
        }

        private static void EnterPostfix(UIInputHandler __instance) => ItemEditorPanel.SetHovered(__instance.gameObject);

        private static void ExitPostfix(UIInputHandler __instance) => ItemEditorPanel.ClearHovered(__instance.gameObject);
    }
}
