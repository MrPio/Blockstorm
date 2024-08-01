using UnityEngine;

namespace UI
{
    /// <summary>
    /// Display the red vignette.
    /// This is only used by the HpHUD script.
    /// </summary>
    public class LowHpHUD : MonoBehaviour
    {
        private const float LowHealthLimit = 0.33f;
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>
        /// Set the alpha according to the player's current health.
        /// </summary>
        /// <param name="hp">hp value normalized in [0,1].</param>
        public void Evaluate(float hp)
        {
            transform.GetChild(0).gameObject.SetActive(hp < LowHealthLimit);
            if (hp < LowHealthLimit)
                canvasGroup.alpha = (1f - hp / LowHealthLimit) / 1.5f;
            else
                canvasGroup.alpha = 0;
        }

        private void Start()
        {
            Evaluate(1f);
        }
    }
}