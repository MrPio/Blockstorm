using Managers;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Partials
{
    public class Spawnable : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team);
            transform.position = spawnPoint + Vector3.up * 10; // TODO: this should be 1
        }
    }
}   