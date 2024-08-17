using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using UnityEngine;

namespace Partials
{
    public class Ragdoll : MonoBehaviour
    {
        private Animator animator;
        private Rigidbody[] ragdollBodies;
        private List<Vector3> _initialPositions = new();
        private List<Quaternion> _initialRotations = new();

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
            animator.enabled = !state;
            ragdollBodies.ToList().ForEach((rb, index) =>
            {
                rb.transform.localPosition = _initialPositions[index];
                rb.transform.localRotation = _initialRotations[index];
                rb.isKinematic = !state;
            });
        }

        public void ApplyForce(string bodyPart, Vector3 force)
        {
            SetRagdollState(true);
            foreach (var body in ragdollBodies.Where(it => it.gameObject.name == bodyPart))
                body.AddForce(force, ForceMode.Impulse);
        }
    }
}