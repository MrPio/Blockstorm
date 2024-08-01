using TMPro;
using UnityEngine;

public class AmmoHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText, ammoLeftText, grenadesText;
    [SerializeField] private GameObject ammoIcon, blockIcon;

    public void SetAmmo(int ammo, int? ammoLeft = null)
    {
        ammoIcon.SetActive(true);
        blockIcon.SetActive(false);
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
        ammoLeftText.gameObject.SetActive(false);
        ammoText.gameObject.SetActive(true);
        ammoText.text = blocks.ToString();
    }

    public void SetGrenades(int grenades)
    {
        grenadesText.text = grenades.ToString();
    }

    public void SetMelee()
    {
        ammoIcon.SetActive(false);
        blockIcon.SetActive(false);
        ammoLeftText.gameObject.SetActive(false);
        ammoText.gameObject.SetActive(false);
    }
}