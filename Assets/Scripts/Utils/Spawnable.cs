using System.Collections;
using Managers;
using UnityEngine;
using VoxelEngine;

namespace Utils
{
    public class Spawnable:MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Spawn());
        }

        private IEnumerator Spawn()
        {
            yield return new WaitForSeconds(0.1f);
            var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team);
            transform.position = spawnPoint + Vector3.up * 10; // TODO: this should be 1
        }
    }
}