using Managers.Serializer;
using Prefabs.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MouseSensitivity : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        private float _lastChange;

        private void Start()
        {
            slider.value = BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/sensitivity", 0.2f);
            slider.onValueChanged.AddListener(value =>
            {
                if (Time.time - _lastChange < 0.1f) return;
                _lastChange = Time.time;
                BinarySerializer.Instance.Serialize(value, ISerializer.ConfigsDir, "sensitivity");
                FindObjectOfType<CameraMovement>().SetSensitivity(value);
            });
        }
    }
}