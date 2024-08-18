using System;
using System.Linq;
using Managers.Firebase;
using Model;
using Network;
using Prefabs;
using Prefabs.Player;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelEngine;
using Logger = UI.Logger;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Ground")] public ParticleSystem blockDigEffect;
        public Transform highlightBlock;
        public Transform placeBlock;

        [Header("Prefabs")] public GameObject playerPrefab;
        public GameObject clientServerManagersPrefab;

        [Header("UI")] public Canvas worldCanvas;
        public Canvas uiCanvas;
        public Transform crosshair;
        public Animator crosshairAnimator;
        public Dashboard dashboard;
        public HpHUD hpHUD;
        public AmmoHUD ammoHUD;
        public ReloadBar reloadBar;
        public ReloadBar staminaBar;
        public Transform circleDamageContainer;
        public Mipmap mipmap;
        public GameObject scopeContainer;
        public CrosshairFire crosshairFire;
        public CrosshairFire scopeFire;
        public GameObject clickToRespawn;
        public GameObject teamSelector;
        public UsernameUI usernameUI;
        public GameObject lobbyMenuUIContainer;
        public GameObject[] lobbyMenuUIs;
        public GameObject loadingBar;
        public GameObject pauseMenu;
        public Logger logger;
        public ScoresHUD scoresHUD;
        public KillPlusOne killPlusOne;

        [Header("Cameras")] public SpawnCamera spawnCamera;
        public SpawnCamera menuCamera;

        [Header("Managers")] public WorldManager worldManager;
        [NonSerialized] public ClientManager ClientManager;
        [NonSerialized] public ServerManager ServerManager;
        public NetworkManager networkManager;
        public LobbyManager lobbyManager;
        public RelayManager relayManager;
        public StorageManager storageManager;

        private void Start()
        {
            var clientServerManagers = Instantiate(clientServerManagersPrefab);
            ClientManager = clientServerManagers.GetComponentInChildren<ClientManager>();
            ServerManager = clientServerManagers.GetComponentInChildren<ServerManager>();
            InitializeMenu();
        }

        /// <summary>
        /// At the beginning, the user has to choose whether he wants to play as a host or as a client.
        /// </summary>
        public void InitializeMenu()
        {
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(false);
            teamSelector.SetActive(false);
            ammoHUD.gameObject.SetActive(false);
            hpHUD.gameObject.SetActive(false);
            mipmap.gameObject.SetActive(false);
            crosshair.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(true);
            lobbyMenuUIContainer.SetActive(true);
            foreach (var lobbyMenuUI in lobbyMenuUIs)
                lobbyMenuUI.SetActive(true);
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(false);
        }

        /// <summary>
        /// When connecting to the lobby.
        /// </summary>
        public void InitializeLoading()
        {
            dashboard.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(true);
            lobbyMenuUIContainer.SetActive(true);
            foreach (var lobbyMenuUI in lobbyMenuUIs)
                lobbyMenuUI.SetActive(false);
            loadingBar.SetActive(true);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(false);
        }

        /// <summary>
        /// This is called right after the player has selected a lobby to join or has created a new one.
        /// </summary>
        public void InitializeMatch(bool isFirstSpawn = true)
        {
            RenderSettings.fog = false;
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(true);
            spawnCamera.InitializePosition();
            teamSelector.SetActive(isFirstSpawn);
            clickToRespawn.SetActive(!isFirstSpawn);
            hpHUD.Reset();
            hpHUD.gameObject.SetActive(false);
            ammoHUD.gameObject.SetActive(false);
            mipmap.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(false);
            lobbyMenuUIContainer.SetActive(false);
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(true);
        }

        /// <summary>
        /// This is called right after the team selection is done and the player is ready to spawn.
        /// </summary>
        public void InitializeSpawn(Team? newTeam = null, bool resetStats = true)
        {
            RenderSettings.fog = true;
            dashboard.gameObject.SetActive(true);
            spawnCamera.gameObject.SetActive(false);
            teamSelector.SetActive(false);
            ammoHUD.gameObject.SetActive(true);
            hpHUD.gameObject.SetActive(true);
            mipmap.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(true);
            menuCamera.gameObject.SetActive(false);
            lobbyMenuUIContainer.SetActive(false);
            FindObjectsOfType<Player>().First(it => it.IsOwner)
                .Spawn(newTeam, resetStats ? new PlayerStats(username: lobbyManager.Username ?? "Debug") : null);
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(true);
        }
    }
}