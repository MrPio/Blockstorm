using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class ServerManager : NetworkBehaviour
    {
        public static ServerManager instance;

        private void Start()
        {
            instance = this;
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerListServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var players = NetworkManager.Singleton.ConnectedClientsList.Select(it => it.ClientId);
            ClientManager.instance.SendPlayerListClientRpc(players.ToArray(), rpcParams.Receive.SenderClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void EditVoxelServerRpc(Vector3 pos, byte newID, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            ClientManager.instance.EditVoxelClientRpc(pos, newID);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void DamageVoxelServerRpc(Vector3 pos, uint damage, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            ClientManager.instance.DamageVoxelClientRpc(pos, damage);
        }
    }
}