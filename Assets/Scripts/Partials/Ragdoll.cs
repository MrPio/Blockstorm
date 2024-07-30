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

        private void ApplyForce(Vector3 force)
        {
            foreach (var body in ragdollBodies)
                body.AddForce(force,ForceMode.Impulse);
        }
    }
}