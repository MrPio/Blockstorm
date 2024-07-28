using UI;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VoxelEngine;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        [SerializeField] private Dashboard dashboard;
        public static ClientManager instance;
        private Transform _highlightBlock;

        private void Start()
        {
            instance = this;
            _highlightBlock = GameObject.FindWithTag("HighlightBlock").transform;
        }

        [ClientRpc]
        public void SendPlayerListClientRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            dashboard.UpdateDashboard(playerIds);
        }

        [ClientRpc]
        public void EditVoxelClientRpc(Vector3 pos, byte newID)
        {
            if (!IsClient) return;
            WorldManager.instance.EditVoxel(pos, newID);
        }

        [ClientRpc]
        public void DamageVoxelClientRpc(Vector3 pos, uint newID)
        {
            if (!IsClient) return;
            if(WorldManager.instance.DamageVoxel(pos, newID))
                _highlightBlock.gameObject.SetActive(false);
        }
    }
}