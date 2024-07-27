using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ServerManager : NetworkBehaviour
{
    public static ServerManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && IsClient)
        {
            print(IsSpawned);
            RequestPlayerListServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayerListServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only allow this to be called by a client
        if (!IsServer) return;

        var players = NetworkManager.Singleton.ConnectedClientsList.Select(it => it.ClientId);
        ClientManager.instance.SendPlayerListClientRpc(players.ToArray(), rpcParams.Receive.SenderClientId);
    }
}