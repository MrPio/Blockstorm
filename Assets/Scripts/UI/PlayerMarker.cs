using Managers;
using Unity.VisualScripting;
using UnityEngine;

namespace UI
{
    public class PlayerMarker : MonoBehaviour
    {
        private SceneManager _sm;
        public Transform player;
        private RectTransform _rectTransform;
        private float _scaleFactor;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _rectTransform = GetComponent<RectTransform>();
            _scaleFactor = transform.parent.GetComponent<RectTransform>().rect.width / _sm.worldManager.Map.size.x;
            InvokeRepeating(nameof(UpdateTransform), 0f, 0.1f);
        }

        /// <summary>
        /// Refresh the marker position and rotation on the minimap.
        /// The marker is automatically destroyed when the player dies.
        /// </summary>
        private void UpdateTransform()
        {
            if (player is null) return;
            if (player.IsDestroyed())
            {
                Destroy(gameObject);
                return;
            }
            _rectTransform.anchoredPosition =
                new Vector2(
                    x: player.position.x * _scaleFactor,
                    y: -(_sm.worldManager.Map.size.z - player.position.z) * _scaleFactor
                );
            _rectTransform.localRotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);
        }
    }
}