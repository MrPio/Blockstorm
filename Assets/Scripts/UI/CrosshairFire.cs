using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UI
{
    public class CrosshairFire : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve;
        [SerializeField] private bool usePosition;
        [SerializeField] private RectTransform[] toAnimate;
        [SerializeField] private float baseIntensity;
        [SerializeField] [CanBeNull] private Volume volume;
        private float _startTime, _duration, _intensity;
        private bool _isAnimating;
        private List<Vector2> startPositions;
        private Vignette vignette;
        private float _baseVignetteIntensity;

        private void Start()
        {
            if (usePosition)
                startPositions = toAnimate.Select(it => it.anchoredPosition).ToList();
            volume?.profile.TryGet(out vignette);
            _baseVignetteIntensity = vignette?.intensity.value ?? 0f;
        }

        private void Update()
        {
            if (!_isAnimating) return;
            var acc = Time.time - _startTime;
            var t = _duration > 1f
                ? acc > 0.2f ? acc / _duration + 0.2f / _duration : acc
                : (acc / _duration);
            var value = baseIntensity * _intensity * animationCurve.Evaluate(t);
            toAnimate.ToList().ForEach((rect, i) =>
            {
                if (usePosition)
                    rect.anchoredPosition =
                        startPositions[i] + Vector2.up.RotateByAngle(rect.rotation.eulerAngles.z) * value;
                else
                    rect.localScale = Vector3.one * (1f + value);
                vignette?.intensity.Override(_baseVignetteIntensity - value / 2f);
            });
            if (t > 1)
                _isAnimating = false;
        }

        public void Animate(float duration, float intensity)
        {
            _isAnimating = true;
            _startTime = Time.time;
            _duration = duration;
            _intensity = intensity;
        }
    }
}