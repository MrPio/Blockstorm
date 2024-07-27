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
        [SerializeField] private List<RectTransform> children = new();
        [SerializeField] private float gap = 1, offset;
        [SerializeField] private bool vertical = true, inverse, force;

        [Button("Update")]
        public void UpdateUI()
        {
            // Retrieve first level children
            if (force || children.Count == 0)
                children = transform.GetComponentsInChildren<RectTransform>(true).ToList()
                    .Where(it => it.parent == transform)
                    .ToList();

            // Arrange the children in a stack
            var accumulator = 0f;
            var activeChildren = children.Where(child => child.gameObject.activeSelf).ToList();
            foreach (var child in activeChildren)
            {
                // Calculate child position
                var childPos = (vertical ? Vector3.up : Vector3.right) * ((accumulator + gap) * (inverse ? -1f : 1f));

                // Assign child position
                child.anchoredPosition = childPos;
                accumulator += vertical ? child.rect.height : child.rect.width;
            }
        }

        private void Awake()
        {
            // Listen on  children enable/disable
            foreach (Transform child in transform)
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