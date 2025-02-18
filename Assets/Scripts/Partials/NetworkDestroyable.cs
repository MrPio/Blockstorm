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
            _sm = FindFirstObjectByType<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                ifIsNotMe.ForEach(Destroy);
            else
                ifIsMe.ForEach(Destroy);
        }

        public void SetEnabled(bool value)
        {
            _sm.logger.Log($"[Active] Player {OwnerClientId} set its active state to '{value}'!",
                IsOwner ? Color.cyan : Color.yellow);
            if (IsOwner)
                ifIsMe.ForEach(o => o.SetActive(value));
            else
                ifIsNotMe.ForEach(o => o.SetActive(value));
        }
    }
}