using System;
using Managers.Serializer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;
using Logger = UI.Logger;

namespace Managers
{
    public class DebugManager : NetworkBehaviour
    {
        private SceneManager _sm;
        private readonly ISerializer _serializer = BinarySerializer.Instance;
        private Logger _logger;
        private bool _isHost;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        private async void Start()
        {
            _sm.lobbyManager.gameObject.SetActive(false);
            _sm.InitializeLoading();
            _logger = FindObjectOfType<Logger>();
            _isHost = _serializer.Deserialize($"{ISerializer.DebugDir}/isHost", true);
            _serializer.Serialize(!_isHost, $"{ISerializer.DebugDir}", "isHost");

            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SignedIn +=
                () => Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (_isHost)
            {
                // var allocation = await RelayService.Instance.CreateAllocationAsync(1);
                // var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                // _logger.Log($"Relay Join Code = {joinCode}");
                // _serializer.Serialize(joinCode, $"{ISerializer.DebugDir}", "relayCode");
                // var relayServerData = new RelayServerData(allocation, "dtls");
                // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                // var relayCode = _serializer.Deserialize($"{ISerializer.DebugDir}/relayCode", "");
                // var allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
                // var relayServerData = new RelayServerData(allocation, "dtls");
                // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }

            FindObjectOfType<SceneManager>().InitializeMatch();

            _logger.Log($"Connection established as {(_isHost ? "Host" : "Client")}");
        }

        public override void OnDestroy()
        {
            if (_isHost)
                _serializer.Serialize(true, $"{ISerializer.DebugDir}", "isHost");
        }
    }
}