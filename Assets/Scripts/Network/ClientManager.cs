using System;
using Prefabs.Player;
using UI;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        private Dashboard _dashboard;
        private Transform _highlightBlock;
        private WorldManager _wm;

        // Used to update the map status
        private readonly NetworkVariable<MapStatus> MapStatus = new(new MapStatus
        {
            Xs = Array.Empty<short>(), Ys = Array.Empty<short>(), Zs = Array.Empty<short>(), Ids = Array.Empty<byte>()
        });

        private void Awake()
        {
            _highlightBlock = GameObject.FindWithTag("HighlightBlock").transform;
            _dashboard = GameObject.FindWithTag("Dashboard").GetComponent<Dashboard>();
            _wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsHost)
            {
                // Request the current map status.
                var mapStatus = MapStatus.Value;
                Debug.Log($"There have been made {mapStatus.Ids.Length} voxel edits to the original map!");
                for (var i = 0; i < mapStatus.Ids.Length; i++)
                    _wm.Map.Blocks[mapStatus.Ys[i], mapStatus.Xs[i], mapStatus.Zs[i]] = mapStatus.Ids[i];
            }

            // Render the map and spawn the player
            // TODO: the player should be spawned after team selection
            _wm.RenderMap();
            Debug.Log($"The map {_wm.Map.name} was rendered!");
            GameObject.FindWithTag("NetworkManager").GetComponent<NetworkManager>().OnClientConnectedCallback +=
                OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log("New client connected: " + clientId);
            if (IsServer)
            {
                var player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            }
        }


        [Rpc(SendTo.Everyone)]
        public void SendPlayerListRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            _dashboard.UpdateDashboard(playerIds);
        }

        /// <summary>
        /// Used to propagate the placement of a block through the network.
        /// </summary>
        /// <param name="pos">The position of the new voxel.</param>
        /// <param name="newID">The type of the new voxel.</param>
        [Rpc(SendTo.Everyone)]
        public void EditVoxelClientRpc(Vector3 pos, byte newID)
        {
            print("EditVoxelClientRpc");
            _wm.EditVoxel(pos, newID);
            if (IsHost)
                MapStatus.Value = new MapStatus(_wm.Map);
        }

        /// <summary>
        /// Used to propagate the damage to a block through the network.
        /// </summary>
        /// <param name="pos">The position of the damaged voxel.</param>
        /// <param name="damage">The damage dealt.</param>
        [Rpc(SendTo.Everyone)]
        public void DamageVoxelRpc(Vector3 pos, uint damage)
        {
            if (_wm.DamageVoxel(pos, damage))
                _highlightBlock.gameObject.SetActive(false);
            if (IsHost)
                MapStatus.Value = new MapStatus(_wm.Map);
        }
    }
}