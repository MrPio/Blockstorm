using UI;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class ClientManager : NetworkBehaviour
    {
        [SerializeField] private Dashboard dashboard;
        public static ClientManager instance;

        private void Awake()
        {
            instance = this;
        }

        [ClientRpc]
        public void SendPlayerListClientRpc(ulong[] playerIds, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            dashboard.UpdateDashboard(playerIds);
        }
    }
}