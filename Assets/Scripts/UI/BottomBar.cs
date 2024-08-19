using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BottomBar : MonoBehaviour
    {
        [SerializeField] private Color selected, unselected;
        [SerializeField] private float showDuration = 3f;
        private CanvasGroup _canvasGroup;
        private readonly Dictionary<WeaponType, Image> _bgs = new();
        private readonly Dictionary<WeaponType, TextMeshProUGUI> _names = new();
        private readonly Dictionary<WeaponType, Image> _images = new();
        private readonly Dictionary<WeaponType, string> _lastWeapons = new();

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            foreach (var weaponType in Enum.GetValues(typeof(WeaponType)).Cast<WeaponType>())
            {
                var item = transform.Find(weaponType.ToString());
                _bgs[weaponType] = item.Find("BG").GetComponent<Image>();
                _names[weaponType] = item.Find("Name").GetComponent<TextMeshProUGUI>();
                _images[weaponType] = item.Find("Weapon").GetComponent<Image>();
            }
        }

        public void Initialize(PlayerStatus? status, WeaponType equipped)
        {
            if (IsInvoking(nameof(Hide)))
                CancelInvoke(nameof(Hide));
            foreach (var weaponType in _bgs.Keys)
            {
                _bgs[weaponType].color = equipped == weaponType ? selected : unselected;
                if (status is null) continue;
                var weapon = status.Value.WeaponType2Weapon(weaponType);

                // Check if the weapon has changed
                if (_lastWeapons.ContainsKey(weaponType) && _lastWeapons[weaponType] == weapon.Name)
                    continue;
                _lastWeapons[weaponType] = weapon.Name;
                _names[weaponType].text = weapon.Name;
                _images[weaponType].sprite = Resources.Load<Sprite>(weapon.GetThumbnail);
            }

            _canvasGroup.alpha = 1;
            InvokeRepeating(nameof(Hide), showDuration, 9999f);
        }

        private void Hide() => _canvasGroup.alpha = 0;
    }
}