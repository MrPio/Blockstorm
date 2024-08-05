using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using ExtensionFunctions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Partials
{
    [ExecuteAlways]
    public class Stack : MonoBehaviour
    {
        [SerializeField] private float gap;
        [SerializeField] private bool vertical = true, inverse, refreshOnActivateChange;
        [SerializeField] private Transform[] childrenToConsider;

        [Button("Update")]
        public void UpdateUI()
        {
            // Arrange the children in a stack
            var accumulator = 0f;
            var count = 0;

            void SetChildPos(Transform child)
            {
                // Calculate child position
                var childPos = (vertical ? Vector3.up : Vector3.right) *
                               ((accumulator + gap * count) * (inverse ? -1f : 1f));
                count++;

                // Assign child position
                var rect = child.GetComponent<RectTransform>();
                rect.anchoredPosition = childPos;
                accumulator += vertical ? rect.rect.height : rect.rect.width;
            }

            if (childrenToConsider is null || childrenToConsider.Length <= 0)
                foreach (Transform child in transform)
                    SetChildPos(child);
            else
                foreach (var child in childrenToConsider.Where(it => it.gameObject.activeSelf))
                    SetChildPos(child);
        }

        private void Awake()
        {
            if (!refreshOnActivateChange)
                return;
            // Listen on children enable/disable
            foreach (var child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.GetComponent<Notifier>() is null)
                    child.AddComponent<Notifier>();
                child.GetComponent<Notifier>().Apply(it =>
                {
                    it.Enabled += UpdateUI;
                    it.Disabled += UpdateUI;
                });
            }
        }

        private void Start() => UpdateUI();
    }
}