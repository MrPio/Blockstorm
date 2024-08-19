using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Managers;
using Model;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Collectable = Prefabs.Collectable;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        private SceneManager _sm;

        // Used to update the map status
        private NetworkVariable<MapStatus> mapStatus = new(new MapStatus
        {
            Xs = Array.Empty<short>(), Ys = Array.Empty<short>(), Zs = Array.Empty<short>(), Ids = Array.Empty<byte>()
        });

        // Used to update the collectables
        public NetworkVariable<CollectablesStatus> collectableStatus =
            new(new CollectablesStatus
            {
                Xs = Array.Empty<float>(), Ys = Array.Empty<float>(), Zs = Array.Empty<float>(),
                CollectableTypes = Array.Empty<byte>(),
                MedkitTypes = Array.Empty<byte>(),
                WeaponTypes = Array.Empty<byte>()
            });

        // Used to update the collectables
        public NetworkVariable<Scores> scores = new();

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
                collectableStatus.OnValueChanged += (_, newValue) => LoadCollectables(newValue);
                LoadCollectables(collectableStatus.Value);

                // Quit to the main menu if the host disconnects or kicks me
                _sm.networkManager.OnClientDisconnectCallback += clientId =>
                {
                    _sm.logger.Log($"Client {clientId} Disconnected!");
                    if (clientId == 0 || clientId == _sm.networkManager.LocalClientId)
                    {
                        NetworkManager.Singleton.Shutdown();
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager
                            .GetActiveScene().buildIndex);
                    }
                };
            }

            // Render the map and spawn the player
            if (IsHost)
            {
                // Spawn the score cube for all the players
                _sm.worldManager.SpawnScoreCube();

                // Spawn collectables on the server and across the net
                _sm.worldManager.SpawnCollectables();
                collectableStatus.Value = new CollectablesStatus(
                    _sm.worldManager.SpawnedCollectables.Select(it => it.transform.position).ToList(),
                    _sm.worldManager.SpawnedCollectables.Select(it => it.Model).ToList());
            }

            // Update the score HUD. If any team wins, quit to the main menu.
            _sm.scoresHUD.Reset();
            scores.OnValueChanged += (_, newValue) =>
            {
                _sm.scoresHUD.SetScores(newValue);
                if (IsHost && newValue.Winner is not null)
                {
                    async void Quit()
                    {
                        NetworkManager.Singleton.Shutdown();
                        await _sm.lobbyManager.LeaveLobby();
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager
                            .GetActiveScene().buildIndex);
                    }

                    Quit();
                }
            };
        }

        /// <summary>
        /// Client-only.
        /// Added the new spawned collectibles and removed the ones that no longer exist
        /// </summary>
        /// <param name="newStatus">The new collectable status received from the server.</param>
        private void LoadCollectables(CollectablesStatus newStatus)
        {
            var newCollectables = newStatus.ToCollectables;
            
            // Spawn the new collectibles. Only collectables with free ids will be spawned.
            foreach (var model in newCollectables)
                _sm.worldManager.SpawnCollectableWithID(model.ID, model);

            // Remove looted collectables
            var removed = new List<Collectable>();
            foreach (var c in _sm.worldManager.SpawnedCollectables.Where(c =>
                         newCollectables.All(it => it.ID != c.Model.ID && !c.IsDestroyed())))
            {
                removed.Add(c);
                _sm.worldManager.FreeCollectablesSpawnPoints.Add(c.Model.ID);
                Destroy(c.gameObject);
            }

            removed.ForEach(it => _sm.worldManager.SpawnedCollectables.Remove(it));
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