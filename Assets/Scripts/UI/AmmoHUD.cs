using TMPro;
using UnityEngine;

namespace UI
{
    public class AmmoHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI ammoText, ammoLeftText, grenadesText, grenadesSecondaryText;
        [SerializeField] private GameObject ammoIcon, blockIcon, rocketIcon, inventoryIcon;

        public void SetAmmo(int ammo, int? ammoLeft = null, bool isTertiary = false)
        {
            ammoIcon.SetActive(!isTertiary);
            blockIcon.SetActive(false);
            rocketIcon.SetActive(isTertiary);
            ammoLeftText.gameObject.SetActive(true);
            ammoText.gameObject.SetActive(true);
            ammoText.text = ammo.ToString();
            if (ammoLeft is not null)
                ammoLeftText.text = ammoLeft.ToString();
        }

        public void SetBlocks(int blocks)
        {
            ammoIcon.SetActive(false);
            blockIcon.SetActive(true);
            rocketIcon.SetActive(false);
            ammoLeftText.gameObject.SetActive(false);
            ammoText.gameObject.SetActive(true);
            ammoText.text = blocks.ToString();
        }

        public void SetGrenades(int grenades, int grenadesSecondary)
        {
            grenadesText.text = grenades.ToString();
            grenadesSecondaryText.text = grenadesSecondary.ToString();
        }

        public void SetMelee()
        {
            ammoIcon.SetActive(false);
            blockIcon.SetActive(false);
            rocketIcon.SetActive(false);
            ammoLeftText.gameObject.SetActive(false);
            ammoText.gameObject.SetActive(false);
        }

        public void SetInventoryIcon(bool value)
        {
            inventoryIcon.SetActive(value);
        }
    }
}