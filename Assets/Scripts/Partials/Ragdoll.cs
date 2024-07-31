using UnityEngine;

namespace Partials
{
    public class Ragdoll : MonoBehaviour
    {
        private Animator animator;
        private Rigidbody[] ragdollBodies;

        private void Start()
        {
            animator = GetComponent<Animator>();
            ragdollBodies = GetComponentsInChildren<Rigidbody>();
            SetRagdollState(false);
        }

        private void SetRagdollState(bool state)
        {
            animator.enabled = !state;
            foreach (var body in ragdollBodies)
                body.isKinematic = !state;
        }

        public void ApplyForce(string bodyPart, Vector3 force)
        {
            SetRagdollState(true);
            foreach (var body in ragdollBodies)
                body.AddForce(force * (body.gameObject.name == bodyPart ? 1f : 0.5f), ForceMode.Impulse);
        }
    }
}