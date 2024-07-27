using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ClientManager : NetworkBehaviour
{
    public static ClientManager instance;

    private void Awake()
    {
        instance = this;
    }

    [ClientRpc]
    public void SendPlayerListClientRpc(ulong[] playerIds, ulong clientId)
    {
        // Only process on the client who requested
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        // Handle the received player list
        foreach (var id in playerIds)
        {
            Debug.Log($"Player ID: {id}");
        }
    }
}