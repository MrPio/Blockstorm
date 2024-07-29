using System.Collections;
using System.Threading;
using UI;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VoxelEngine;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        private Dashboard _dashboard;
        public static ClientManager instance;
        private Transform _highlightBlock;

        private void Start()
        {
            instance = this;
            _highlightBlock = GameObject.FindWithTag("HighlightBlock").transform;
            _dashboard = GameObject.FindWithTag("Dashboard").GetComponent<Dashboard>();
        }

        [ClientRpc]
        public void SendPlayerListClientRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            _dashboard.UpdateDashboard(playerIds);
        }

        /// <summary>
        /// Used to propagate the placement of a block through the network.
        /// </summary>
        /// <param name="pos">The position of the new voxel.</param>
        /// <param name="newID">The type of the new voxel.</param>
        [ClientRpc]
        public void EditVoxelClientRpc(Vector3 pos, byte newID)
        {
            if (!IsClient) return;
            WorldManager.instance.EditVoxel(pos, newID);
        }

        /// <summary>
        /// Used to propagate the damage to a block through the network.
        /// </summary>
        /// <param name="pos">The position of the damaged voxel.</param>
        /// <param name="damage">The damage dealt.</param>
        [ClientRpc]
        public void DamageVoxelClientRpc(Vector3 pos, uint damage)
        {
            if (!IsClient) return;
            if (WorldManager.instance.DamageVoxel(pos, damage))
                _highlightBlock.gameObject.SetActive(false);
        }
    }
}