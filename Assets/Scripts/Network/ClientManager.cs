using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Unity.Netcode;
using UnityEngine;

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
        public readonly NetworkVariable<CollectablesStatus> collectableStatus =
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

                // React on collectable changes
                collectableStatus.OnValueChanged += (_, newValue) =>
                {
                    var newCollectables = newValue.ToCollectables;
                    foreach (var model in newCollectables)
                    {
                        if (_sm.worldManager.SpawnedCollectables.Any(it => it.Model.ID == model.ID)) continue;
                        var collectableGo = Instantiate(collectable, model.ID, Quaternion.identity)
                            .GetComponent<Prefabs.Collectable>();
                        _sm.worldManager.SpawnedCollectables.Add(collectableGo);
                        collectableGo.Initialize(model);
                    }

                    foreach (var spawnedCollectable in _sm.worldManager.SpawnedCollectables)
                        if (newCollectables.All(it => it.ID != spawnedCollectable.Model.ID))
                            Destroy(spawnedCollectable);
                };
            }

            // Render the map and spawn the player
            // TODO: the player should be spawned after team selection
            _sm.worldManager.RenderMap();
            if (IsHost)
            {
                // Spawn collectables on the server and across the net
                _sm.worldManager.SpawnCollectables();
                _sm.clientManager.collectableStatus.Value = new CollectablesStatus(
                    _sm.worldManager.SpawnedCollectables.Select(it => it.transform.position).ToList(),
                    _sm.worldManager.SpawnedCollectables.Select(it => it.Model).ToList());
            }

            Debug.Log($"The map {_sm.worldManager.Map.name} was rendered!");
            _sm.networkManager.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log("New client connected: " + clientId);
            if (IsServer)
            {
                var player = Instantiate(_sm.playerPrefab);
                player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            }
        }


        [Rpc(SendTo.Everyone)]
        public void SendPlayerListRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            _sm.dashboard.UpdateDashboard(playerIds);
        }

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