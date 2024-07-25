using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExtensionFunctions;

namespace Partials
{
    public class TabMenu : MonoBehaviour
    {
        [SerializeField] private Color enabledColor, disabledColor;
        [SerializeField] private List<GameObject> sections;
        private List<Button> _tabs = new();
        private List<TextMeshProUGUI> _tabTexts = new();

        public int CurrentIndex
        {
            set =>
                _tabs.ForEach((it, i) =>
                {
                    sections[i].SetActive(i == value);
                    _tabTexts[i].color = i == value ? enabledColor : disabledColor;
                });
        }

        private void Start()
        {
            _tabs = transform.GetComponentsInChildren<Button>().ToList();
            _tabTexts = _tabs.Select(it => it.GetComponentInChildren<TextMeshProUGUI>()).ToList();
            _tabs.ForEach((it, i) => it.onClick.AddListener(() => CurrentIndex = i));
            CurrentIndex = 0;
        }

        private void OnEnable() => CurrentIndex = 0;
    }
}