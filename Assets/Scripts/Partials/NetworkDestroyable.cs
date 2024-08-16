using System.Collections.Generic;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Partials
{
    public class NetworkDestroyable : NetworkBehaviour
    {
        private SceneManager _sm;

        [SerializeField] private List<GameObject> ifIsMe = new(), ifIsNotMe = new();

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            ifIsMe.ForEach(o =>
            {
                if (!IsOwner)
                    Destroy(o);
            });
            ifIsNotMe.ForEach(o =>
            {
                if (IsOwner)
                    Destroy(o);
            });
        }

        public void SetEnabled(bool value)
        {
            _sm.logger.Log($"[Active] Player {OwnerClientId} set its active state to '{value}'!");
            if (IsOwner)
                ifIsMe.ForEach(o => o.SetActive(value));
            else
                ifIsNotMe.ForEach(o => o.SetActive(value));
        }
    }
}