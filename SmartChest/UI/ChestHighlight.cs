using UnityEngine;

namespace SmartChest.UI
{
    /// <summary>
    /// Fait briller un coffre quelques secondes (même méthode qu'AzuAutoStore :
    /// WearNTear.Highlight() appelé à chaque image).
    /// </summary>
    public class ChestHighlight : MonoBehaviour
    {
        private WearNTear _wnt;
        private float _until;

        public static void Flash(Container container, float seconds)
        {
            if (container == null) return;
            ChestHighlight h = container.gameObject.GetComponent<ChestHighlight>();
            if (h == null) h = container.gameObject.AddComponent<ChestHighlight>();
            h._until = Time.time + seconds;
        }

        private void Awake()
        {
            _wnt = GetComponent<WearNTear>();
            if (_wnt == null) _wnt = GetComponentInParent<WearNTear>();
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
    }
}
