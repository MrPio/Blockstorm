using UnityEngine;

public class Bullet : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        GetComponent<Rigidbody>().velocity=Vector3.zero;
    }
}
