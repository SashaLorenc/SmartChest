using System.Collections.Generic;
using Jotunn.Managers;
using SmartChest.Storing;
using UnityEngine;
using UnityEngine.UI;

namespace SmartChest.UI
{
    /// <summary>Petite fenêtre : taper le nom d'un objet, les coffres proches qui en contiennent brillent.</summary>
    public static class SearchPanel
    {
        private const float Width = 460f;

        private static GameObject _panel;
        private static InputField _input;
        private static string _lastQuery = "";

        public static bool IsOpen => _panel != null;

        public static void Open()
        {
            Panels.CloseAll();

            _panel = UiKit.CreatePanel(Width, 170f);
            UiKit.AddText(_panel.transform, Localization.instance.Localize("$smartchest_search_title"), 20f, 15f, Width - 40f, 30f, 20,
                GUIManager.Instance.ValheimOrange);

            _input = UiKit.AddInput(_panel.transform, _lastQuery, Localization.instance.Localize("$smartchest_search_placeholder"),
                25f, 60f, Width - 50f, 34f);
            // Entrée dans le champ = lancer la recherche
            _input.onEndEdit.AddListener(text =>
            {
                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) Search();
            });

            UiKit.AddButton(_panel.transform, "$smartchest_search_go", 25f, 110f, 200f, 34f, Search);
            UiKit.AddButton(_panel.transform, "$smartchest_close", Width - 25f - 150f, 110f, 150f, 34f, Close);

            _input.ActivateInputField();
        }

        private static void Search()
        {
            if (_input == null) return;
            _lastQuery = _input.text.Trim();
            Player player = Player.m_localPlayer;
            if (player == null || _lastQuery.Length == 0) return;

            List<ChestSearch.Hit> hits = ChestSearch.Find(player.transform.position, Plugin.SearchRadius.Value, _lastQuery);

            ChestHighlight.ClearAll();
            float duration = Plugin.SearchDuration.Value;
            foreach (ChestSearch.Hit hit in hits)
                ChestHighlight.Flash(hit.Container, duration, $"{hit.ItemName} x{hit.Count}");

            string msg = hits.Count == 0
                ? string.Format(Localization.instance.Localize("$smartchest_search_none"), _lastQuery)
                : string.Format(Localization.instance.Localize("$smartchest_search_found"), _lastQuery, hits.Count);
            player.Message(MessageHud.MessageType.Center, msg);
        }

        public static void Close()
        {
            if (_panel != null) Object.Destroy(_panel);
            _panel = null;
            _input = null;
        }
    }
}
