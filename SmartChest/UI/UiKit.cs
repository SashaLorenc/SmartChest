using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartChest.UI
{
    /// <summary>Petits utilitaires partagés par les panneaux du mod (Jötunn GUIManager).</summary>
    internal static class UiKit
    {
        internal static readonly string[] Palette =
            { "E67E22", "2ECC71", "3498DB", "9B59B6", "E91E63", "1ABC9C", "F1C40F", "E74C3C", "00BCD4", "8BC34A",
              "A0703C", "B5B5B5", "FFD700", "7F8C8D", "D9534F", "4CAF50" };

        /// <summary>Vrai quand un champ de texte a le focus : on bloque alors les raccourcis du jeu.</summary>
        internal static bool IsTyping
        {
            get
            {
                EventSystem es = EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                InputField input = es.currentSelectedGameObject.GetComponent<InputField>();
                return input != null && input.isFocused;
            }
        }

        internal static string NextColor(string current)
        {
            int i = System.Array.IndexOf(Palette, (current ?? "").ToUpperInvariant());
            return Palette[(i + 1) % Palette.Length];
        }

        internal static Transform PanelRoot =>
            GUIManager.CustomGUIFront != null ? GUIManager.CustomGUIFront.transform : InventoryGui.instance.transform;

        internal static GameObject CreatePanel(float width, float height)
        {
            return GUIManager.Instance.CreateWoodpanel(
                parent: PanelRoot,
                anchorMin: new Vector2(1f, 0.5f),
                anchorMax: new Vector2(1f, 0.5f),
                position: new Vector2(-width / 2f - 30f, 0f),
                width: width,
                height: height,
                draggable: true);
        }

        internal static Toggle AddToggle(Transform parent, float x, float y, bool isOn, UnityEngine.Events.UnityAction<bool> onChange)
        {
            GameObject go = GUIManager.Instance.CreateToggle(parent, 26f, 26f);
            Place(go, x, y, 26f, 26f);
            Toggle toggle = go.GetComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onChange);
            return toggle;
        }

        internal static InputField AddInput(Transform parent, string text, string placeholder, float x, float y, float width, float height)
        {
            GameObject go = GUIManager.Instance.CreateInputField(
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                contentType: InputField.ContentType.Standard,
                placeholderText: placeholder,
                fontSize: 18,
                width: width,
                height: height);
            Place(go, x, y, width, height);
            InputField input = go.GetComponent<InputField>();
            input.characterLimit = 32;
            input.text = text ?? "";
            return input;
        }


        /// <summary>Place un élément depuis le coin haut-gauche de son parent (x vers la droite, y vers le bas).</summary>
        internal static void Place(GameObject go, float x, float y, float width, float height)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        internal static GameObject AddButton(Transform parent, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize(label),
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                width: width,
                height: height);
            Place(go, x, y, width, height);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            return go;
        }

        internal static void SetButtonText(GameObject button, string text)
        {
            if (button == null) return;
            Text t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        internal static void AddText(Transform parent, string text, float x, float y, float width, float height, int size, Color color)
        {
            GameObject go = GUIManager.Instance.CreateText(
                text: text,
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: size,
                color: color,
                outline: true,
                outlineColor: Color.black,
                width: width,
                height: height,
                addContentSizeFitter: false);
            Place(go, x, y, width, height);
            Text t = go.GetComponent<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.alignment = TextAnchor.MiddleLeft;
            }
        }
    }
}
