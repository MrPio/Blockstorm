using System;
using Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventoryTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private Image bg;
        private Inventory _inventory;
        private readonly Color hoverColor = new(1f, 0.68f, 0f);
        private readonly Color selectedColor = new(1f, 1f, 1f, 0.2f);
        private readonly Color unselectedColor = Color.clear;

        private void Awake()
        {
            _inventory = FindObjectOfType<Inventory>();
        }

        private void Start()
        {
            UpdateUI();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bg.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            bg.color = _inventory.selectedWeaponType == weaponType ? selectedColor : unselectedColor;
        }

        public void UpdateUI()
        {
            _inventory ??= FindObjectOfType<Inventory>();
            bg.color = _inventory.selectedWeaponType == weaponType ? selectedColor : unselectedColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _inventory.selectedWeaponType = weaponType;
            _inventory.UpdateUI(isChangingType: true);
        }
    }
}