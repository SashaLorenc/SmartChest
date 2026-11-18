using Jotunn.Managers;
using UnityEngine;

namespace SmartChest.UI
{
    /// <summary>
    /// Résultat de recherche sur un coffre : il brille quelques secondes (même méthode qu'AzuAutoStore :
    /// WearNTear.Highlight() à chaque image) et affiche « Objet xN » collé sur sa face tournée vers la caméra,
    /// au centre du coffre, pour qu'on sache à quel coffre il appartient même quand ils sont empilés.
    /// </summary>
    public class ChestHighlight : MonoBehaviour
    {
        private WearNTear _wnt;
        private float _until;
        private GameObject _labelGo;
        private TextMesh _label;
        private Bounds _bounds;
        private bool _hasBounds;

        public static void Flash(Container container, float seconds, string text)
        {
            if (container == null) return;
            ChestHighlight h = container.gameObject.GetComponent<ChestHighlight>();
            if (h == null) h = container.gameObject.AddComponent<ChestHighlight>();
            h._until = Time.time + seconds;
            h.SetText(text);
        }

        /// <summary>Efface tous les résultats d'une recherche précédente.</summary>
        public static void ClearAll()
        {
            // On n'appelle pas Destroy ici : un coffre retrouvé par la nouvelle recherche réutilise le même composant
            foreach (ChestHighlight h in FindObjectsOfType<ChestHighlight>()) h._until = 0f;
        }

        private void Awake()
        {
            _wnt = GetComponent<WearNTear>();
            if (_wnt == null) _wnt = GetComponentInParent<WearNTear>();
            ComputeBounds();
        }

        /// <summary>Boîte englobante des modèles 3D du coffre (avant d'ajouter notre texte).</summary>
        private void ComputeBounds()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                // Seulement les modèles 3D (pas les effets de particules, flammes...)
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (!_hasBounds)
                {
                    _bounds = r.bounds;
                    _hasBounds = true;
                }
                else _bounds.Encapsulate(r.bounds);
            }
        }

        private void SetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_labelGo == null)
            {
                _labelGo = new GameObject("SmartChestSearchLabel");
                _labelGo.transform.SetParent(transform, false);
                _label = _labelGo.AddComponent<TextMesh>();
                _label.richText = true;
                _label.fontSize = 40;
                _label.characterSize = 0.03f;
                _label.anchor = TextAnchor.MiddleCenter;
                _label.alignment = TextAlignment.Center;
                _label.color = new Color(1f, 1f, 0.4f);

                Font font = GUIManager.Instance != null ? GUIManager.Instance.AveriaSerifBold : null;
                if (font != null)
                {
                    _label.font = font;
                    MeshRenderer renderer = _labelGo.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.material = font.material;
                }
            }
            _label.text = text;
        }

        private void Update()
        {
            if (Time.time > _until)
            {
                Destroy(this);
                return;
            }
            if (_wnt != null) _wnt.Highlight();
        }

        private void LateUpdate()
        {
            if (_labelGo == null || GameCamera.instance == null) return;
            Transform cam = GameCamera.instance.transform;

            Vector3 center = _hasBounds ? _bounds.center : transform.position + Vector3.up * 0.5f;
            Vector3 toCam = cam.position - center;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) toCam = -cam.forward;
            Vector3 dir = toCam.normalized;

            // Point de la face du coffre tournée vers la caméra, à la hauteur du centre, un peu devant
            float depth = 0.5f;
            if (_hasBounds && _bounds.IntersectRay(new Ray(center + dir * 10f, -dir), out float hit))
                depth = 10f - hit;
            _labelGo.transform.position = center + dir * (depth + 0.05f);
            _labelGo.transform.rotation = cam.rotation;
        }

        private void OnDestroy()
        {
            if (_labelGo != null) Destroy(_labelGo);
        }
    }
}
