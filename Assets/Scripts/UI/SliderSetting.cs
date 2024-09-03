using System;
using System.Collections.Generic;
using Managers.Serializer;
using Prefabs.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelEngine;

namespace UI
{
    [Serializable]
    public enum SliderSettingType
    {
        MouseSensitivity,
        RenderDistance,
        Fov,
    }

    public class SliderSetting : MonoBehaviour
    {
        public static readonly Dictionary<SliderSettingType, string> configFiles = new()
        {
            { SliderSettingType.MouseSensitivity, "sensitivity" },
            { SliderSettingType.RenderDistance, "render_distance" },
            { SliderSettingType.Fov, "fov" }
        };

        public static readonly Dictionary<SliderSettingType, float> defaultValues = new()
        {
            { SliderSettingType.MouseSensitivity, 0.2f },
            { SliderSettingType.RenderDistance, 0.75f },
            { SliderSettingType.Fov, 0.5f }
        };

        public static readonly Dictionary<SliderSettingType, float> delays = new()
        {
            { SliderSettingType.MouseSensitivity, 0.025f },
            { SliderSettingType.RenderDistance, 0.025f },
            { SliderSettingType.Fov, 0.025f }
        };
        
        public static readonly Dictionary<SliderSettingType, int> steps = new()
        {
            { SliderSettingType.MouseSensitivity, 16 },
            { SliderSettingType.RenderDistance, 6 },
            { SliderSettingType.Fov, 8 }
        };


        [SerializeField] private Slider slider;
        [SerializeField] private SliderSettingType sliderSettingType;
        private float _lastChange;

        private void Start()
        {
            slider.maxValue = steps[sliderSettingType];
            slider.value =
                BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/{configFiles[sliderSettingType]}",
                    defaultValues[sliderSettingType]*steps[sliderSettingType]);
            slider.onValueChanged.AddListener(value =>
            {
                if (!slider.wholeNumbers && Time.time - _lastChange < delays[sliderSettingType]) return;
                _lastChange = Time.time;
                UpdateSetting(value / slider.maxValue);
            });
        }

        private void UpdateSetting(float value)
        {
            BinarySerializer.Instance.Serialize(value * slider.maxValue, ISerializer.ConfigsDir,
                configFiles[sliderSettingType]);
            if (sliderSettingType is SliderSettingType.MouseSensitivity)
                FindObjectOfType<CameraMovement>().SetSensitivity(value);
            else if (sliderSettingType is SliderSettingType.RenderDistance)
                FindObjectOfType<WorldManager>().SetRenderDistance(value);
            else if (sliderSettingType is SliderSettingType.Fov)
                FindObjectOfType<CameraMovement>().SetFOV(value);
        }
    }
}