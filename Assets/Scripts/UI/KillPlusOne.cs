using System;
using Model;
using TMPro;
using UnityEngine;
using VoxelEngine;

namespace UI
{
    public class KillPlusOne : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI plusOne;
        [SerializeField] private GameObject crosshairHit;
        [SerializeField] private float crosshairHitDuration, allDuration;
        private int _killAcc;

        private void Start()
        {
            crosshairHit.SetActive(false);
            plusOne.gameObject.SetActive(false);
        }

        public void Activate(Team killTeam, bool isSuicide, bool isKill = true)
        {
            if (IsInvoking(nameof(DeactivateCrosshairHit)))
                CancelInvoke(nameof(DeactivateCrosshairHit));
            if (IsInvoking(nameof(DeactivateAll)))
                CancelInvoke(nameof(DeactivateAll));

            crosshairHit.SetActive(true);
            crosshairHit.transform.localScale = Vector3.one * (isKill ? 1f : 0.85f);
            if (isKill)
            {
                if (isSuicide)
                    _killAcc = 0;
                else
                    _killAcc++;
                plusOne.gameObject.SetActive(true);
                plusOne.color = TeamData.Colors[killTeam];
                plusOne.text = $"+{_killAcc}";
                InvokeRepeating(nameof(DeactivateAll), allDuration, 99999f);
            }

            InvokeRepeating(nameof(DeactivateCrosshairHit), crosshairHitDuration * (isKill ? 1f : 0.35f), 99999f);
        }

        private void DeactivateCrosshairHit() =>
            crosshairHit.SetActive(false);

        private void DeactivateAll()
        {
            CancelInvoke(nameof(DeactivateAll));
            CancelInvoke(nameof(DeactivateCrosshairHit));
            plusOne.gameObject.SetActive(false);
            _killAcc = 0;
        }
    }
}