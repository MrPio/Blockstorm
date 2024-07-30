using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// The game is not server-authoritative;
    /// however, there are some cases in which the server must give info to clients as it is protected.
    /// For example, the list of connected players required in the Dashboard can be accessed only by the server.
    /// </summary>
    public class ServerManager : NetworkBehaviour
    {
        private ClientManager _clientManager;

        private void Awake()
        {
            _clientManager = GameObject.FindWithTag("ClientServerManagers").GetComponentInChildren<ClientManager>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerListServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var players = NetworkManager.Singleton.ConnectedClientsList.Select(it => it.ClientId);
            _clientManager.SendPlayerListRpc(players.ToArray(), rpcParams.Receive.SenderClientId);
        }
    }
}