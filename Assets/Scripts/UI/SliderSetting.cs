using System;
using System.Collections.Generic;
using Managers;
using Managers.Serializer;
using Prefabs.Player;
using UnityEngine;
using UnityEngine.Audio;
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
        Volume
    }

    public class SliderSetting : MonoBehaviour
    {
        private SceneManager _sm;

        public static readonly Dictionary<SliderSettingType, string> configFiles = new()
        {
            { SliderSettingType.MouseSensitivity, "sensitivity" },
            { SliderSettingType.RenderDistance, "render_distance" },
            { SliderSettingType.Fov, "fov" },
            { SliderSettingType.Volume, "volume" }
        };

        public static readonly Dictionary<SliderSettingType, float> defaultValues = new()
        {
            { SliderSettingType.MouseSensitivity, 0.2f },
            { SliderSettingType.RenderDistance, 0.75f },
            { SliderSettingType.Fov, 0.5f },
            { SliderSettingType.Volume, 1f }
        };

        public static readonly Dictionary<SliderSettingType, int> steps = new()
        {
            { SliderSettingType.MouseSensitivity, 16 },
            { SliderSettingType.RenderDistance, 6 },
            { SliderSettingType.Fov, 8 },
            { SliderSettingType.Volume, 20 }
        };


        [SerializeField] private Slider slider;
        [SerializeField] private SliderSettingType sliderSettingType;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            slider.maxValue = steps[sliderSettingType];
            slider.value =
                BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/{configFiles[sliderSettingType]}",
                    defaultValues[sliderSettingType] * steps[sliderSettingType]);
            slider.onValueChanged.AddListener(value => UpdateSetting(value / slider.maxValue));
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
            else if (sliderSettingType is SliderSettingType.Volume)
                _sm.audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1.0f)) * 20);
        }
    }
}