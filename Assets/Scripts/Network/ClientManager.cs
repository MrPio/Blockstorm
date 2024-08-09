using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Collectable = Prefabs.Collectable;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        private SceneManager _sm;

        // Used to update the map status
        private readonly NetworkVariable<MapStatus> mapStatus = new(new MapStatus
        {
            Xs = Array.Empty<short>(), Ys = Array.Empty<short>(), Zs = Array.Empty<short>(), Ids = Array.Empty<byte>()
        });

        // Used to update the collectables
        public readonly NetworkVariable<CollectablesStatus> CollectableStatus =
            new(new CollectablesStatus
            {
                Xs = Array.Empty<float>(), Ys = Array.Empty<float>(), Zs = Array.Empty<float>(),
                CollectableTypes = Array.Empty<byte>(),
                MedkitTypes = Array.Empty<byte>(),
                WeaponNames = Array.Empty<NetString>()
            });

        [SerializeField] private GameObject collectable;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsHost)
            {
                // Request the current map status.
                var status = mapStatus.Value;
                Debug.Log($"There have been made {status.Ids.Length} voxel edits to the original map!");
                for (var i = 0; i < status.Ids.Length; i++)
                    _sm.worldManager.Map.Blocks[status.Ys[i], status.Xs[i], status.Zs[i]] = status.Ids[i];

                void LoadCollectables(CollectablesStatus newStatus)
                {
                    var newCollectables = newStatus.ToCollectables;
                    foreach (var model in newCollectables)
                        _sm.worldManager.SpawnCollectableWithID(model.ID, model);

                    var removed = new List<Collectable>();
                    foreach (var spawnedCollectable in _sm.worldManager.SpawnedCollectables)
                        if (newCollectables.All(it =>
                                it.ID != spawnedCollectable.Model.ID && !spawnedCollectable.IsDestroyed()))
                        {
                            print($"Destroying {spawnedCollectable.Model.ID}");
                            removed.Add(spawnedCollectable);
                            _sm.worldManager.FreeCollectablesSpawnPoints.Add(spawnedCollectable.Model.ID);
                            Destroy(spawnedCollectable.gameObject);
                        }

                    removed.ForEach(it => _sm.worldManager.SpawnedCollectables.Remove(it));
                }

                // React on collectable changes
                CollectableStatus.OnValueChanged += (_, newValue) => LoadCollectables(newValue);
                LoadCollectables(CollectableStatus.Value);
            }

            // Render the map and spawn the player
            _sm.worldManager.RenderMap();
            if (IsHost)
            {
                // Spawn collectables on the server and across the net
                _sm.worldManager.SpawnCollectables();
                _sm.clientManager.CollectableStatus.Value = new CollectablesStatus(
                    _sm.worldManager.SpawnedCollectables.Select(it => it.transform.position).ToList(),
                    _sm.worldManager.SpawnedCollectables.Select(it => it.Model).ToList());
            }

            Debug.Log($"The map {_sm.worldManager.Map.name} was rendered!");
            // _sm.networkManager.OnClientConnectedCallback += OnClientConnected;
        }

        /*private void OnClientConnected(ulong clientId)
        {
            Debug.Log("New client connected: " + clientId);
            if (IsServer)
            {
                var player = Instantiate(_sm.playerPrefab);
                player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            }
        }*/


        /*[Rpc(SendTo.Everyone)]
        public void SendPlayerListRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            _sm.dashboard.UpdateDashboard(playerIds);
        }*/

        /// <summary>
        /// Used to propagate the placement of a block through the network.
        /// </summary>
        /// <param name="positions">The position of the new voxel.</param>
        /// <param name="newID">The type of the new voxel.</param>
        [Rpc(SendTo.Everyone)]
        public void EditVoxelClientRpc(Vector3[] positions, byte newID)
        {
            _sm.worldManager.EditVoxels(positions.ToList(), newID);
            if (IsHost)
                mapStatus.Value = new MapStatus(_sm.worldManager.Map);
        }

        /// <summary>
        /// Used to propagate the damage to a block through the network.
        /// </summary>
        /// <param name="pos">The position of the damaged voxel.</param>
        /// <param name="damage">The damage dealt.</param>
        [Rpc(SendTo.Everyone)]
        public void DamageVoxelRpc(Vector3 pos, uint damage)
        {
            if (_sm.worldManager.DamageVoxel(pos, damage))
                _sm.highlightBlock.gameObject.SetActive(false);
            if (IsHost)
                mapStatus.Value = new MapStatus(_sm.worldManager.Map);
        }
    }
}