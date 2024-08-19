using System;
using Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventoryWeaponTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image bg;
        private Inventory _inventory;
        private readonly Color selectedColor = new(1f, 0.68f, 0f);
        private readonly Color hoverColor = new(1f, 1f, 1f, 0.2f);
        private readonly Color unselectedColor = new(1f, 1f, 1f, 0.1f);
        [NonSerialized] public Weapon Weapon;

        private void Awake()
        {
            _inventory = FindObjectOfType<Inventory>();
        }

        private void Start()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            _inventory ??= FindObjectOfType<Inventory>();
            bg.color = _inventory.selectedWeapons[_inventory.selectedWeaponType] == Weapon
                ? selectedColor
                : unselectedColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bg.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UpdateUI();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _inventory.selectedWeapons[_inventory.selectedWeaponType] = Weapon;
            _inventory.UpdateUI();
        }
    }
}