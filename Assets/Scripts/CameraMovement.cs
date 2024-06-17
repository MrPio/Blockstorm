using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float sensX, sensY;
    public Transform orientation;
    private float _rotX, _rotY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        var mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;
        _rotY += mouseX;
        _rotX -= mouseY;
        _rotX = Mathf.Clamp(_rotX, -90f, 90f);
        _rotX = Mathf.Clamp(_rotX, -90f, 90f);
        transform.rotation = Quaternion.Euler(_rotX, _rotY, 0);
        orientation.rotation = Quaternion.Euler(0, _rotY, 0);
    }
}