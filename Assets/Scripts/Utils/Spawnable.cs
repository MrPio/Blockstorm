using System;
using Managers;
using UnityEngine;
using VoxelEngine;

namespace Utils
{
    public class Spawnable:MonoBehaviour
    {
        private void Start()
        {
            var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team);
            transform.position = spawnPoint + Vector3.up * 10; // TODO: this should be 1
        }
    }
}