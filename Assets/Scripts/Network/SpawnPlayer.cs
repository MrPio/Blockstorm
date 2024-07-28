using Managers;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Network
{
    public class SpawnPlayer : NetworkBehaviour
    {
        public GameObject prefab;


        /*public override void OnDestroy()
        {
            if (IsServer)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }*/

        public override void OnNetworkSpawn()
        {
            print("OnNetworkSpawn()");
            if (IsServer)
            {
                // OnClientConnected(OwnerClientId);
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            print(clientId);
            var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team) +
                             Vector3.up * 0.15f;
            var player = Instantiate(prefab, spawnPoint, Quaternion.Euler(0, Random.Range(-180f, 180f), 0));
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }

        /*private IEnumerator SetPositionAfterSpawn()
        {
            yield return new WaitForSeconds(1);
            yield return null; // Wait for the next frame
            var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team);
            transform.position = spawnPoint + Vector3.up * 10; // TODO: this should be 1
            print($"Spawned at {spawnPoint}, {IsSpawned}");
        }*/
    }
}