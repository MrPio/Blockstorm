using System;
using System.Collections;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Prefabs;
using Prefabs.Player;
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
        public void SpawnPrefabServerRpc(string prefabName, NetVector3 position, NetVector3 rotation)
        {
            var go = Instantiate(networkPrefabsList.PrefabList.First(it => it.Prefab.name == prefabName).Prefab,
                position.ToVector3,
                Quaternion.Euler(rotation.ToVector3)
            );
            go.GetComponent<NetworkObject>().Spawn(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnExplosiveServerRpc(string prefabName, NetVector3 position, NetVector3 rotation,
            NetVector3 direction, uint damage, float explosionTime, float explosionRange, float groundDamageFactor,
            float force = 0, ServerRpcParams rpcParams = default)
        {
            var go = Instantiate(networkPrefabsList.PrefabList.First(it => it.Prefab.name == prefabName).Prefab,
                position.ToVector3,
                Quaternion.Euler(rotation.ToVector3)
            );
            go.GetComponent<NetworkObject>().SpawnWithOwnership(0);
            go.GetComponent<Explosive>().InitializeRpc(direction, damage, explosionTime, explosionRange,
                groundDamageFactor, rpcParams.Receive.SenderClientId, force);
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
            _sm.ClientManager.collectableStatus.Value = new CollectablesStatus(
                _sm.worldManager.SpawnedCollectables.Select(it => it.transform.position).ToList(),
                _sm.worldManager.SpawnedCollectables.Select(it => it.Model).ToList());
        }
    }
}