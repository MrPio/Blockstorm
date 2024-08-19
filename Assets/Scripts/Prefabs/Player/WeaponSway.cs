using System;
using UnityEngine;

namespace Prefabs.Player
{
    public class WeaponSway : MonoBehaviour
    {
        [Header("Sway Settings")] [SerializeField]
        private float smooth = 7;

        [SerializeField] private float multiplier = 2.5f;
        [SerializeField] private bool advanced;
        private Vector3 _lastPos;

        private void Start()
        {
            _lastPos = transform.position;
        }

        private void Update()
        {
            if (Weapon.isAiming)
                return;

            if (!advanced)
            {
                // get mouse input
                var mouseX = Input.GetAxisRaw("Mouse X") * multiplier;
                var mouseY = Input.GetAxisRaw("Mouse Y") * multiplier;
                var x = Input.GetAxis("Horizontal");
                var z = Input.GetAxis("Vertical");

                // calculate target rotation
                var rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
                var rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

                var rotationX2 = Quaternion.AngleAxis(z * 5f * (Weapon.isAiming ? 0.4f : 1f), Vector3.right);
                var rotationY2 = Quaternion.AngleAxis(x * 5f * (Weapon.isAiming ? 0.4f : 1f), Vector3.up);

                var targetRotation = rotationX * rotationY * rotationX2 * rotationY2;

                // rotate 
                transform.localRotation =
                    Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
            }
            else
            {
                // get movement
                var delta = (transform.position - _lastPos).normalized * multiplier;
                _lastPos = transform.position;

                // calculate target rotation
                var rotationX = Quaternion.AngleAxis(delta.x, Vector3.right);
                var rotationY = Quaternion.AngleAxis(delta.y, Vector3.up);
                var rotationZ = Quaternion.AngleAxis(delta.z, Vector3.forward);

                var rotationX2 = Quaternion.AngleAxis(delta.x * 5f * (Weapon.isAiming ? 0.4f : 1f), Vector3.right);
                var rotationY2 = Quaternion.AngleAxis(delta.y * 5f * (Weapon.isAiming ? 0.4f : 1f), Vector3.up);
                var rotationZ2 = Quaternion.AngleAxis(delta.z * 5f * (Weapon.isAiming ? 0.4f : 1f), Vector3.forward);

                var targetRotation = rotationX * rotationY * rotationZ * rotationX2 * rotationY2 * rotationZ2;

                // rotate 
                transform.localRotation =
                    Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
            }
        }
    }
}