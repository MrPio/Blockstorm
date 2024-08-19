using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Mathematics.math;

namespace UI
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private Transform bodyContent;
        [SerializeField] private GameObject weaponTilePrefab;
        [SerializeField] private Image footerImage;

        [SerializeField]
        private TextMeshProUGUI footerDamageText, footerRofText, footerDistanceText, footerMagazineText, footerAmmoText;

        [SerializeField] private Slider footerDamageSlider,
            footerRofSlider,
            footerDistanceSlider,
            footerMagazineSlider,
            footerAmmoSlider;

        [SerializeField] private InventoryTab[] inventoryTabs;

        [NonSerialized] public WeaponType selectedWeaponType;
        [NonSerialized] public Dictionary<WeaponType,Weapon> selectedWeapons=new();

        private void OnEnable()
        {
            selectedWeaponType = WeaponType.Melee;
            selectedWeapons[WeaponType.Melee] = Weapon.Melees[0];
            selectedWeapons[WeaponType.Primary] = Weapon.Primaries[0];
            selectedWeapons[WeaponType.Secondary] = Weapon.Secondaries[0];
            selectedWeapons[WeaponType.Tertiary] = Weapon.Tertiaries[0];
            selectedWeapons[WeaponType.Grenade] = Weapon.Grenades[0];
            selectedWeapons[WeaponType.GrenadeSecondary] = Weapon.GrenadesSecondary[0];
            UpdateUI(true);
        }

        public void UpdateUI(bool isChangingType = false)
        {
            foreach (RectTransform child in bodyContent)
                if (isChangingType)
                    Destroy(child.gameObject);
                else
                    child.GetComponent<InventoryWeaponTile>().UpdateUI();

            if (isChangingType)
            {
                foreach (var inventoryTab in inventoryTabs)
                    inventoryTab.UpdateUI();
                var weapons = Weapon.Weapons.Where(it => it.Type == selectedWeaponType && it.Variant == null).ToList();
                foreach (var weapon in weapons)
                {
                    var weaponTile = Instantiate(weaponTilePrefab, bodyContent).transform;
                    weaponTile.Find("Header").Find("WeaponName").GetComponent<TextMeshProUGUI>().text = weapon.Name;
                    weaponTile.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>(weapon.GetThumbnail);
                    weaponTile.GetComponent<InventoryWeaponTile>().Weapon = weapon;
                }
            }

            footerImage.sprite = Resources.Load<Sprite>(selectedWeapons[selectedWeaponType].GetThumbnail);
            footerDamageText.text = selectedWeapons[selectedWeaponType].Damage == 0 ? "-" : selectedWeapons[selectedWeaponType].Damage.ToString();
            footerRofText.text = selectedWeapons[selectedWeaponType].Rof == 0 ? "-" : selectedWeapons[selectedWeaponType].Rof.ToString();
            footerDistanceText.text = selectedWeapons[selectedWeaponType].Distance == 0 ? "-" : selectedWeapons[selectedWeaponType].Distance.ToString();
            footerMagazineText.text = selectedWeapons[selectedWeaponType].Magazine?.ToString() ?? "-";
            footerAmmoText.text = selectedWeapons[selectedWeaponType].Ammo?.ToString() ?? "-";

            footerDamageSlider.value = min(250f, selectedWeapons[selectedWeaponType].Damage / 250f);
            footerRofSlider.value = min(600f, selectedWeapons[selectedWeaponType].Rof / 600f);
            footerDistanceSlider.value = min(300f, selectedWeapons[selectedWeaponType].Distance / 300f);
            footerMagazineSlider.value = min(100f, (selectedWeapons[selectedWeaponType].Magazine ?? 0) / 100f);
            footerAmmoSlider.value = min(400f, (selectedWeapons[selectedWeaponType].Ammo ?? 0) / 400f);
        }
    }
}