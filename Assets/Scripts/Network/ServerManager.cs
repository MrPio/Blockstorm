using System.Collections;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Prefabs;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace Network
{
    /// <summary>
    /// The game is not server-authoritative;
    /// however, there are some cases in which the server must give info to clients as it is protected.
    /// For example, the list of connected players required in the Dashboard can be accessed only by the server.
    /// </summary>
    public class ServerManager : NetworkBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private NetworkPrefabsList networkPrefabsList;

        [SerializeField] private GameObject playerPrefab;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerListServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var players = NetworkManager.Singleton.ConnectedClientsList.Select(it => it.ClientId);
            _sm.clientManager.SendPlayerListRpc(players.ToArray(), rpcParams.Receive.SenderClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespawnServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            StartCoroutine(Respawn());
            return;

            IEnumerator Respawn()
            {
                yield return new WaitForSeconds(5f / 2f);
                Destroy(GameObject.FindGameObjectsWithTag("Player").First(it =>
                    it.GetComponent<NetworkObject>().OwnerClientId == rpcParams.Receive.SenderClientId));
                yield return null;
                var player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId, true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPrefabServerRpc(string prefabName, NetVector3 position, NetVector3 rotation)
        {
            if (!IsServer) return;
            var go = Instantiate(networkPrefabsList.PrefabList.First(it => it.Prefab.name == prefabName).Prefab,
                position.ToVector3,
                Quaternion.Euler(rotation.ToVector3)
            );
            go.GetComponent<NetworkObject>().SpawnWithOwnership(0);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnExplosiveServerRpc(string prefabName, NetVector3 position, NetVector3 rotation,
            NetVector3 forward, uint damage, float explosionTime, float explosionRange, float force = 0,
            ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var go = Instantiate(networkPrefabsList.PrefabList.First(it => it.Prefab.name == prefabName).Prefab,
                position.ToVector3,
                Quaternion.Euler(rotation.ToVector3)
            );
            go.GetComponent<NetworkObject>().SpawnWithOwnership(0);
            go.GetComponent<Explosive>().InitializeRpc(forward, damage, explosionTime, explosionRange,
                rpcParams.Receive.SenderClientId, force);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LootCollectableServerRpc(NetVector3 id)
        {
            var newId = _sm.worldManager.FreeCollectablesSpawnPoints.RandomItem();
            _sm.worldManager.FreeCollectablesSpawnPoints.Add(id);
            var looted = _sm.worldManager.SpawnedCollectables.First(it => it.Model.ID == id);
            _sm.worldManager.SpawnedCollectables.Remove(looted);
            Destroy(looted.gameObject);

            // Spawn new collectable on the host
            _sm.worldManager.SpawnCollectableWithID(newId); 

            // Spawn new collectable on the clients
            _sm.clientManager.collectableStatus.Value = new CollectablesStatus(
                _sm.worldManager.SpawnedCollectables.Select(it => it.transform.position).ToList(),
                _sm.worldManager.SpawnedCollectables.Select(it => it.Model).ToList());
        }
    }
}