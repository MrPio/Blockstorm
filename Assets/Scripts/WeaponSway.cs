using System;
using UnityEngine;

public class WeaponSway : MonoBehaviour {

    [Header("Sway Settings")]
    [SerializeField] private float smooth=7;
    [SerializeField] private float multiplier=2.5f;

    private void Update()
    {
        // get mouse input
        var mouseX = Input.GetAxisRaw("Mouse X") * multiplier;
        var mouseY = Input.GetAxisRaw("Mouse Y") * multiplier;
        var x = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");

        // calculate target rotation
        var rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        var rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);
        
        var rotationX2 = Quaternion.AngleAxis(z*5f, Vector3.right);
        var rotationY2 = Quaternion.AngleAxis(x*5f, Vector3.up);

        var targetRotation = rotationX * rotationY*rotationX2*rotationY2;

        // rotate 
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
    }
}
