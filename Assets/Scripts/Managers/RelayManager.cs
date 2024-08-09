using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using UnityEngine;

namespace Managers
{
    public class RelayManager : MonoBehaviour
    {
        private SceneManager _sm;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        // For the host
        public async Task<string> CreateRelay()
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(LobbyManager.MaxPlayers - 1);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"Relay Join Code = {joinCode}");

                // Integrate Relay with Netcode for game objects
                var relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        // For the clients
        public async Task JoinRelay(string joinCode)
        {
            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                // Integrate Relay with Netcode for game objects
                var relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }
}