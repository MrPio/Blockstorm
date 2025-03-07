using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Partials
{
    public class Ragdoll : MonoBehaviour
    {
        private Animator animator;
        private Rigidbody[] ragdollBodies;
        private List<Vector3> _initialPositions = new();
        private List<Quaternion> _initialRotations = new();
        private static readonly int Reset = Animator.StringToHash("reset");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            ragdollBodies = GetComponentsInChildren<Rigidbody>();
            _initialPositions = new List<Vector3>();
            _initialRotations = new List<Quaternion>();
            foreach (var body in ragdollBodies)
            {
                _initialPositions.Add(body.transform.localPosition);
                _initialRotations.Add(body.transform.localRotation);
            }

            SetRagdollState(false);
        }

        public void SetRagdollState(bool state)
        {
            animator.enabled = false;
            var index = 0;
            foreach (var rb in ragdollBodies)
            {
                rb.isKinematic = !state;
                if (!state)
                {
                    rb.transform.localPosition = _initialPositions[index];
                    rb.transform.localRotation = _initialRotations[index];
                    index++;
                }
            }

            animator.enabled = !state;
            if (!state)
                animator.SetTrigger(Reset);
        }

        public void ApplyForce(string bodyPart, Vector3 force)
        {
            SetRagdollState(true);
            ragdollBodies.First(it => it.gameObject.name == bodyPart).AddForce(force, ForceMode.Impulse);
        }
    }
}