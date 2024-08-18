using System;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class HpHUD : MonoBehaviour
    {
        [Serializable]
        private class ColorRule
        {
            public Color color;
            public int belowHp;
        }

        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private ColorRule[] colorRules;
        [SerializeField] private GameObject helmetIcon, armorIcon;
        [SerializeField] private LowHpHUD lowHpHUD;

        public void SetHp(int hp, int armor, bool? hasHelmet = null)
        {
            hp = math.max(hp, 0);
            armor = math.max(armor, 0);
            lowHpHUD.Evaluate(hp / 100f);
            hpText.text = hp.ToString();
            hpText.color = colorRules.First(it => it.belowHp >= hp).color;
            armorIcon.SetActive(armor > 0);
            if (hasHelmet is not null)
                helmetIcon.SetActive(hasHelmet.Value);
        }

        public void Reset() => SetHp(100,50, true);
    }
}