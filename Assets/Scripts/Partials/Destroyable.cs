using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Partials
{
    public class Destroyable : MonoBehaviour
    {
        public float lifespan = 30;
        [SerializeField] private bool destroyOnTrigger = false;

        private float _startTime;

        private void Start()
        {
            _startTime = Time.time;
        }

        private void FixedUpdate()
        {
            if (lifespan > 0.001f && Time.time - _startTime > lifespan)
            {
                if (gameObject == null || gameObject.IsDestroyed())
                    return;
                var networkObject = GetComponent<NetworkObject>();
                if (networkObject is null || NetworkManager.Singleton.IsHost)
                    Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (destroyOnTrigger)
                Destroy(gameObject);
        }
    }
}