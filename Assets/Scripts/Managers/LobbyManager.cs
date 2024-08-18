using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Managers.Serializer;
using TMPro;
using UI;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Managers
{
    public class LobbyManager : MonoBehaviour
    {
        private SceneManager _sm;

        [SerializeField] private Transform lobbiesContainer;
        [SerializeField] private GameObject lobbyPrefab;
        [SerializeField] private GameObject nothingToShow;
        [NonSerialized] public const ushort MaxPlayers = 32;
        [NonSerialized] public string Username;

        // The lobby is automatically destroyed after 30 seconds.
        public const float HeartbeatRate = 15f;
        public const char PasswordFillChar = '0';
        public Lobby HostedLobby;
        private Lobby _joinedLobby;
        private List<Lobby> _lobbies;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        private async void Start()
        {
            await Initialize();
            InvokeRepeating(nameof(UpdateLobbies), 0.1f, 2f);
            InvokeRepeating(nameof(SendHeartbeat), 1f, HeartbeatRate);
        }

        private async Task UpdateLobbies()
        {
            _lobbies = await ListLobbies();
            Debug.Log($"Found {_lobbies.Count} Lobbies");
            nothingToShow.SetActive(_lobbies.Count == 0);
            foreach (Transform child in lobbiesContainer)
                if (child != nothingToShow.transform)
                    DestroyImmediate(child.gameObject);
            foreach (var lobby in _lobbies)
            {
                var lobbyGo = Instantiate(lobbyPrefab, lobbiesContainer);
                lobbyGo.GetComponentInChildren<ClickableUI>().LobbyId = lobby.Id;
                lobbyGo.transform.Find("MapTitle").GetComponent<TextMeshProUGUI>().text =
                    $"{lobby.Data["map"].Value} ({lobby.Data["author"].Value})";
                lobbyGo.transform.Find("Players").GetComponent<TextMeshProUGUI>().text =
                    $"{lobby.Players.Count}/{lobby.MaxPlayers}";
                lobbyGo.transform.Find("Code").GetComponent<TextMeshProUGUI>().text = lobby.LobbyCode;
            }
        }

        /// <summary>
        /// Anonymously connect to AuthenticationService to enable Lobby managment.
        /// </summary>
        private async Task Initialize()
        {
            Username = BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/username",
                $"Player{Random.Range(10, 10000)}");
            _sm.usernameUI.Initialize();

            var options = new InitializationOptions();
            // options.SetProfile(Username);
            await UnityServices.InitializeAsync(options);
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
            AuthenticationService.Instance.SignedIn +=
                () => Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Create a new lobby. The player that hosts the lobby became the host.
        /// </summary>
        /// <param name="lobbyName"> The name of the lobby. </param>
        /// <param name="gameMode"> The type of the game. Defaults to 4x8 death match.</param>
        /// <param name="map"> The name of the map. Defaults to "Harbor".</param>
        /// <param name="password"> The lobby password. If not provided, the lobby is public. </param>
        public async Task CreateLobby(string lobbyName, string gameMode = "4 Teams",
            string map = "Harbor", string password = null)
        {
            try
            {
                await _sm.worldManager.RenderMap(map);
                var relayCode = await _sm.relayManager.CreateRelay();
                var options = new CreateLobbyOptions()
                {
                    Player = GetPlayerOptions(),
                    IsPrivate = password is not null,
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            "game_mode",
                            new DataObject(DataObject.VisibilityOptions.Public, gameMode, DataObject.IndexOptions.S1)
                        },
                        { "map", new DataObject(DataObject.VisibilityOptions.Public, map, DataObject.IndexOptions.S2) },
                        {
                            "author",
                            new DataObject(DataObject.VisibilityOptions.Public, Username, DataObject.IndexOptions.S3)
                        },
                        { "relay_code", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                };
                if (password is not null)
                    options.Password = password.Length >= 8
                        ? password
                        : password + new string(PasswordFillChar, 8 - password.Length);
                HostedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);
                _joinedLobby = HostedLobby;
                Debug.Log($"Lobby '{HostedLobby.Name}' created!");
                CancelInvoke(nameof(UpdateLobbies));
                while (!_sm.worldManager.HasRendered)
                    await Task.Delay(500);
                _sm.InitializeMatch();
                return;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
                _sm.InitializeMenu();
                return;
            }
        }

        /// <summary>
        /// Quit the joined lobby. Stop sending the heartbeat.
        /// </summary>
        /// <remarks> Lobbies have automatic host migration. </remarks>
        public async Task LeaveLobby()
        {
            CancelInvoke(nameof(SendHeartbeat));
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

            // If the there is at least another player, migrate the lobby host
            if (HostedLobby is not null)
            {
                if (HostedLobby.Players.Count > 1)
                    HostedLobby = await Lobbies.Instance.UpdateLobbyAsync(HostedLobby.Id,
                        new UpdateLobbyOptions { HostId = HostedLobby.Players[1].Id });
                HostedLobby = null;
            }

            _joinedLobby = null;
        }

        /// <summary>
        /// List the available lobbies
        /// </summary>
        /// <returns> The list of available lobbies</returns>
        public async Task<List<Lobby>> ListLobbies(string gameMode = "4 Teams")
        {
            try
            {
                var response = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new(QueryFilter.FieldOptions.S1, gameMode, QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder>
                    {
                        new(true, QueryOrder.FieldOptions.AvailableSlots)
                    }
                });
                return response.Results;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
                return new List<Lobby>();
            }
        }

        /// <summary>
        /// Join a lobby by code. The lobby code is much shorter than its id.
        /// </summary>
        /// <param name="code"> The code of the lobby. </param>
        /// <param name="password"> The password of the lobby, if private. </param>
        public async Task<bool> JoinLobbyByCode(string code, string password = null)
        {
            try
            {
                var options = new JoinLobbyByCodeOptions()
                {
                    Player = GetPlayerOptions(),
                };
                if (password is not null)
                    options.Password = password.Length >= 8
                        ? password
                        : password + new string(PasswordFillChar, 8 - password.Length);
                _joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code.ToUpper(), options);
                CancelInvoke(nameof(UpdateLobbies));
                await _sm.worldManager.RenderMap(_joinedLobby.Data["map"].Value);
                await _sm.relayManager.JoinRelay(_joinedLobby.Data["relay_code"].Value);
                while (!_sm.worldManager.HasRendered)
                    await Task.Delay(500);
                _sm.InitializeMatch();
                return true;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
                _sm.InitializeMenu();
                return false;
            }
        }

        public async Task<bool> JoinLobbyById(string id)
        {
            try
            {
                var options = new JoinLobbyByIdOptions()
                {
                    Player = GetPlayerOptions(),
                };
                _joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(id, options);
                CancelInvoke(nameof(UpdateLobbies));
                await _sm.worldManager.RenderMap(_joinedLobby.Data["map"].Value);
                await _sm.relayManager.JoinRelay(_joinedLobby.Data["relay_code"].Value);
                while (!_sm.worldManager.HasRendered)
                    await Task.Delay(500);
                _sm.InitializeMatch();
                return true;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
                _sm.InitializeMenu();
                return false;
            }
        }

        /// <summary>
        /// Quick join a lobby without specifying any code nor id.
        /// </summary>
        public async void QuickJoinLobby()
        {
            try
            {
                _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                CancelInvoke(nameof(UpdateLobbies));
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        /// <summary>
        /// Update the data stored in the lobby.
        /// </summary>
        /// <param name="gameMode"> The new game mode</param>
        /// <param name="map"> The new map</param>
        /// <remarks>
        /// The other players won't automatically see the changes, so a "check for update" polling should be
        /// also implemented to refresh the joinedLobby value. Note that the API limits to 1 request per second.
        /// </remarks>
        public async void UpdateHostedLobby(string gameMode, string map, string relayCode)
        {
            try
            {
                HostedLobby = await Lobbies.Instance.UpdateLobbyAsync(HostedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "game_mode", new(DataObject.VisibilityOptions.Public, gameMode, DataObject.IndexOptions.S1) },
                        { "map", new(DataObject.VisibilityOptions.Public, map, DataObject.IndexOptions.S2) },
                        { "relay_code", new(DataObject.VisibilityOptions.Member, relayCode) },
                    }
                });
                PrintLobby(HostedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        private async void SendHeartbeat()
        {
            if (_joinedLobby is null) return;
            if (HostedLobby is null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                _sm.logger.Log("Migration has occurred! You are now the host of this lobby!");
                HostedLobby = _joinedLobby;
            }
            if (HostedLobby is not null)
            {
                _sm.logger.Log("Sending Heartbeat...");
                await LobbyService.Instance.SendHeartbeatPingAsync(HostedLobby.Id);
            }
        }

        private Player GetPlayerOptions() => new(id: AuthenticationService.Instance.PlayerId)
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Username) }
            }
        };

        private void PrintLobby(Lobby lobby)
        {
            Debug.Log(
                $"Players in Lobby: {lobby.Name}. Game mode: {lobby.Data["game_mode"].Value}. Map: {lobby.Data["map"].Value}");
            foreach (var player in lobby.Players)
                Debug.Log(player.Id + " " + player.Data["username"].Value);
        }

        private async void OnDestroy()
        {
            print("exiting...");
            if (_joinedLobby is not null)
                await LeaveLobby();
        }
    }
}