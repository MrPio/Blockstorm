using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Random = UnityEngine.Random;

public class LobbyManager : MonoBehaviour
{
    [NonSerialized] public const ushort MaxPlayers = 32;
    [NonSerialized] public string Username;

    // The lobby is automatically destroyed after 30 seconds.
    public const float HeartbeatRate = 15f;
    private Lobby _hostedLobby, _joinedLobby;

    /// <summary>
    /// Anonymously connect to AuthenticationService to enable Lobby managment.
    /// </summary>
    private async void Initialize()
    {
        Username = $"Player{Random.Range(0, 1000)}";
        var options = new InitializationOptions();
        options.SetProfile(Username);
        await UnityServices.InitializeAsync(options);
        AuthenticationService.Instance.SignedIn +=
            () => Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /// <summary>
    /// Create a new lobby. The player that hosts the lobby became the host.
    /// </summary>
    /// <param name="lobbyName"> The name of the lobby. </param>
    /// <param name="relayCode"> The relay to which the player should connect to. </param>
    /// <param name="gameMode"> The type of the game. Defaults to 4x8 death match.</param>
    /// <param name="map"> The name of the map. Defaults to "Harbor".</param>
    /// <param name="password"> The lobby password. If not provided, the lobby is public. </param>
    public async void CreateLobby(string lobbyName, string relayCode, string gameMode = "4 Teams", string map = "Harbor",
        string password = null)
    {
        try
        {
            var options = new CreateLobbyOptions()
            {
                Player = GetPlayerOptions(),
                Password = password,
                IsPrivate = password is not null,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "game_mode",
                        new DataObject(DataObject.VisibilityOptions.Public, gameMode, DataObject.IndexOptions.S1)
                    },
                    { "map", new DataObject(DataObject.VisibilityOptions.Public, map, DataObject.IndexOptions.S2) },
                    { "relay_code", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            };
            _hostedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);
            InvokeRepeating(nameof(SendHeartbeat), 1f, 15f);
            Debug.Log($"Lobby '{_hostedLobby.Name}' created!");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Quit the hosted lobby. Stop sending the heartbeat.
    /// </summary>
    /// <remarks> Lobbies have automatic host migration. </remarks>
    public async void LeaveHostedLobby(bool isHost)
    {
        if (isHost)
        {
            CancelInvoke(nameof(SendHeartbeat));
            _hostedLobby = null;
        }

        await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
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
                    new(false, QueryOrder.FieldOptions.MaxPlayers)
                }
            });
            Debug.Log($"Found {response.Results.Count} Lobbies");
            foreach (var lobby in response.Results)
                Debug.Log($"--> {lobby.Name} ");
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
    public async void JoinLobby(string code, string password = null)
    {
        try
        {
            var options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayerOptions(),
                Password = password
            };
            _joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);
            PrintLobby(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
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
            _hostedLobby = await Lobbies.Instance.UpdateLobbyAsync(_hostedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "game_mode", new(DataObject.VisibilityOptions.Public, gameMode, DataObject.IndexOptions.S1) },
                    { "map", new(DataObject.VisibilityOptions.Public, map, DataObject.IndexOptions.S2) },
                    { "relay_code", new(DataObject.VisibilityOptions.Member, relayCode) },
                }
            });
            PrintLobby(_hostedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void SendHeartbeat()
    {
        if (_hostedLobby is not null)
            await LobbyService.Instance.SendHeartbeatPingAsync(_hostedLobby.Id);
    }

    private Player GetPlayerOptions() => new()
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
}