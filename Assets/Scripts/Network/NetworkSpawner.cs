using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class NetworkSpawner : NetworkBehaviour
    {
        [SerializeField] private List<GameObject> prefabs;

        public override void OnNetworkSpawn()
        {
            prefabs.ForEach(o => Instantiate(o));
        }
    }
}