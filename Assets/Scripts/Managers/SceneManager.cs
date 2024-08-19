using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Managers.Firebase;
using Model;
using Network;
using Prefabs;
using Prefabs.Player;
using UI;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;
using Logger = UI.Logger;
using Weapon = Model.Weapon;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Ground")] public ParticleSystem blockDigEffect;
        public Transform highlightBlock;
        public Transform placeBlock;
        public Transform highlightArea;

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
        public GameObject inventory;
        public UsernameUI usernameUI;
        public GameObject lobbyMenuUIContainer;
        public GameObject[] lobbyMenuUIs;
        public GameObject loadingBar;
        public GameObject pauseMenu;
        public Logger logger;
        public ScoresHUD scoresHUD;
        public KillPlusOne killPlusOne;
        public BottomBar bottomBar;
        public GameObject invincibilityHUD;

        [Header("Cameras")] public SpawnCamera spawnCamera;
        public SpawnCamera menuCamera;

        [Header("Managers")] public WorldManager worldManager;
        [NonSerialized] public ClientManager ClientManager;
        [NonSerialized] public ServerManager ServerManager;
        public NetworkManager networkManager;
        public LobbyManager lobbyManager;
        public RelayManager relayManager;
        public StorageManager storageManager;
        private Team? _newTeam = null;
        [CanBeNull] private Dictionary<WeaponType, Weapon> _lastSelectedWeapons = null;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(false);
            teamSelector.SetActive(false);
            inventory.SetActive(false);
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
            invincibilityHUD.SetActive(false);
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
            invincibilityHUD.SetActive(false);
        }

        /// <summary>
        /// This is called right after the player has selected a lobby to join or has created a new one.
        /// </summary>
        public void InitializeTeamSelection(bool isFirstSpawn = true)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RenderSettings.fog = false;
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(true);
            spawnCamera.InitializePosition();
            teamSelector.SetActive(isFirstSpawn);
            inventory.SetActive(false);
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
            invincibilityHUD.SetActive(false);
        }

        /// <summary>
        /// This is called right after the team selection.
        /// </summary>
        public void InitializeInventory(Team? newTeam = null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RenderSettings.fog = false;
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(true);
            teamSelector.SetActive(false);
            inventory.gameObject.SetActive(true);
            ammoHUD.gameObject.SetActive(false);
            hpHUD.gameObject.SetActive(false);
            mipmap.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(false);
            lobbyMenuUIContainer.SetActive(false);
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(true);
            invincibilityHUD.SetActive(false);
            _newTeam = newTeam;
        }

        /// <summary>
        /// This is called right after the inventory selection and the player is ready to spawn.
        /// </summary>
        public void InitializeSpawn(bool resetStats = true, Dictionary<WeaponType, Weapon> selectedWeapons = null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            var player = FindObjectsOfType<Player>().First(it => it.IsOwner);
            player.Spawn(_newTeam, resetStats ? new PlayerStats(username: lobbyManager.Username ?? "Debug") : null);
            if (selectedWeapons is not null || _lastSelectedWeapons is not null)
            {
                selectedWeapons ??= _lastSelectedWeapons;
                _lastSelectedWeapons = selectedWeapons;
                var newStatus = player.Status.Value;
                newStatus.Melee = selectedWeapons[WeaponType.Melee];
                newStatus.Primary = selectedWeapons[WeaponType.Primary];
                newStatus.Secondary = selectedWeapons[WeaponType.Secondary];
                newStatus.Tertiary = selectedWeapons[WeaponType.Tertiary];
                newStatus.Grenade = selectedWeapons[WeaponType.Grenade];
                newStatus.GrenadeSecondary = selectedWeapons[WeaponType.GrenadeSecondary];
                player.Status.Value = newStatus;

                // Initialize the BottomBar
                bottomBar.Initialize(newStatus, WeaponType.Block);
            }

            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
            scoresHUD.gameObject.SetActive(true);
            inventory.SetActive(false);
        }
    }
}