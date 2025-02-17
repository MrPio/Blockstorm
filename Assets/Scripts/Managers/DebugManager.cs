using Managers.Serializer;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
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

        [SerializeField] private bool isMultiplayer;
        [SerializeField] private string mapName;

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
        }

        private async void Start()
        {
            _sm.lobbyManager.gameObject.SetActive(false);
            _sm.InitializeLoading();
            _logger = FindFirstObjectByType<Logger>();
            if (isMultiplayer)
            {
                _isHost = _serializer.Deserialize($"{ISerializer.DebugDir}/isHost", true);
                _serializer.Serialize(!_isHost, $"{ISerializer.DebugDir}", "isHost");
                await UnityServices.InitializeAsync();
                if (AuthenticationService.Instance.IsSignedIn)
                    AuthenticationService.Instance.SignOut();
                AuthenticationService.Instance.SignedIn +=
                    () => Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (_isHost)
                {
                    await _sm.worldManager.RenderMap(mapName);
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
                    await _sm.worldManager.RenderMap(mapName);
                    // var relayCode = _serializer.Deserialize($"{ISerializer.DebugDir}/relayCode", "");
                    // var allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
                    // var relayServerData = new RelayServerData(allocation, "dtls");
                    // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                    NetworkManager.Singleton.StartClient();
                    _logger.Log($"Connection established as {(_isHost ? "Host" : "Client")}");
                }
            }
            else
                await _sm.worldManager.RenderMap(mapName);

            FindFirstObjectByType<SceneManager>().InitializeTeamSelection();
        }

        public override void OnDestroy()
        {
            if (_isHost)
                _serializer.Serialize(true, $"{ISerializer.DebugDir}", "isHost");
        }
    }
}