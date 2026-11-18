using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using SmartChest.Catalog;
using SmartChest.Chests;
using SmartChest.Compat;
using SmartChest.Storing;
using UnityEngine;

namespace SmartChest.UI
{
    /// <summary>
    /// Étiquette flottante au-dessus d'un coffre catégorisé (catégories en couleur, « plein », résultats de recherche).
    /// Ajoutée à chaque coffre par ContainerRegistry ; ne crée son texte que si elle a quelque chose à afficher.
    /// </summary>
    public class ChestLabel : MonoBehaviour
    {
        private const float RefreshDelay = 0.5f;

        private Container _container;
        private GameObject _go;
        private TextMesh _text;
        private float _nextRefresh;
        private string _shown;

        private void Awake()
        {
            _container = GetComponent<Container>();
            // Étale les rafraîchissements des coffres sur plusieurs images
            _nextRefresh = Time.time + Random.value * RefreshDelay;
        }

        private void Update()
        {
            if (Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + RefreshDelay;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_go == null || !_go.activeSelf) return;
            _go.transform.position = transform.position + Vector3.up * Plugin.LabelHeight.Value;
            // Face à la caméra
            if (GameCamera.instance != null) _go.transform.rotation = GameCamera.instance.transform.rotation;
        }

        private void Refresh()
        {
            string text = ComputeText();
            if (text == null)
            {
                if (_go != null && _go.activeSelf) _go.SetActive(false);
                return;
            }

            if (_go == null) Create();
            if (!_go.activeSelf) _go.SetActive(true);
            if (text != _shown)
            {
                _text.text = text;
                _shown = text;
            }
            _text.characterSize = 0.05f * Plugin.LabelScale.Value;
        }

        private string ComputeText()
        {
            if (_container == null) return null;
            Player player = Player.m_localPlayer;
            if (player == null) return null;
            float dist = Vector3.Distance(player.transform.position, transform.position);

            // Résultat de recherche : affiché même si les étiquettes sont désactivées
            string hit = ChestSearch.GetHitText(_container);
            if (hit != null && dist <= Plugin.SearchRadius.Value)
            {
                string normal = Plugin.ShowLabels.Value ? Describe(_container, true) : null;
                return "<color=#FFFF66>» " + hit + " «</color>" + (string.IsNullOrEmpty(normal) ? "" : "\n" + normal);
            }

            if (!Plugin.ShowLabels.Value || dist > Plugin.LabelDistance.Value) return null;
            if (!ChestCategories.IsSmart(_container) || !ModCompat.IsEligibleChest(_container)) return null;
            string desc = Describe(_container, true);
            return string.IsNullOrEmpty(desc) ? null : desc;
        }

        /// <summary>
        /// Description courte d'un coffre : ses catégories en couleur, « + le reste » s'il reçoit le reste.
        /// Avec extras : priorité et « plein ».
        /// </summary>
        public static string Describe(Container container, bool extras)
        {
            List<string> parts = ChestCategories.Get(container)
                .Select(CatalogManager.GetCategory)
                .Where(c => c != null)
                .Select(c => $"<color=#{c.Color}>{c.DisplayName}</color>")
                .ToList();

            string text = string.Join(", ", parts);
            if (ChestCategories.GetMode(container) == ChestMode.Overflow)
            {
                string rest = Localization.instance.Localize(parts.Count == 0 ? "$smartchest_mode_overflow" : "$smartchest_label_rest");
                text += (text.Length > 0 ? " " : "") + "<color=#CCCCCC>" + rest + "</color>";
            }
            if (!extras || text.Length == 0) return text;

            int prio = ChestCategories.GetPriority(container);
            if (prio > 0) text += " <color=#CCCCCC>(" + Localization.instance.Localize("$smartchest_priority") + " " + prio + ")</color>";
            if (ChestCategories.IsFull(container))
                text += "\n<color=#FF5050>" + Localization.instance.Localize("$smartchest_full").ToUpperInvariant() + "</color>";
            return text;
        }

        private void Create()
        {
            _go = new GameObject("SmartChestLabel");
            _go.transform.SetParent(transform, false);
            _text = _go.AddComponent<TextMesh>();
            _text.richText = true;
            _text.fontSize = 40;
            _text.characterSize = 0.05f * Plugin.LabelScale.Value;
            _text.anchor = TextAnchor.LowerCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;

            Font font = GUIManager.Instance != null ? GUIManager.Instance.AveriaSerifBold : null;
            if (font != null)
            {
                _text.font = font;
                MeshRenderer renderer = _go.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = font.material;
            }
        }

        private void OnDestroy()
        {
            if (_go != null) Destroy(_go);
        }
    }
}
