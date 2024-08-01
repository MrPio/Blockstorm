using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ReloadBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        private bool _isReloading;
        private float _accumulator, _duration;

        private void Start() => slider.gameObject.SetActive(false);

        private void Update()
        {
            if (!_isReloading) return;
            var value = _accumulator / _duration;
            if (value < 1)
                slider.value = value;
            else
                Stop();
            _accumulator += Time.deltaTime;
        }

        public void Reload(float duration)
        {
            _isReloading = true;
            slider.gameObject.SetActive(true);
            _duration = duration;
            _accumulator = 0;
        }

        public void Stop()
        {
            _isReloading = false;
            slider.gameObject.SetActive(false);
        }
    }
}