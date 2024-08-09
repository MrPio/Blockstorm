﻿using System;
using Model;
using Network;
using Prefabs;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelEngine;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Ground")] public ParticleSystem blockDigEffect;
        public Transform highlightBlock;
        public Transform placeBlock;

        [Header("Prefabs")] public GameObject playerPrefab;

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

        [Header("Cameras")] public SpawnCamera spawnCamera;
        public SpawnCamera menuCamera;

        [Header("Managers")] public WorldManager worldManager;
        public ClientManager clientManager;
        public ServerManager serverManager;
        public NetworkManager networkManager;
        public LobbyManager lobbyManager;
        public RelayManager relayManager;

        private void Start()
        {
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
        }

        /// <summary>
        /// This is called right after the player has selected a lobby to join or has created a new one.
        /// </summary>
        public void InitializeMatch()
        {
            dashboard.gameObject.SetActive(false);
            spawnCamera.gameObject.SetActive(true);
            spawnCamera.InitializePosition();
            teamSelector.SetActive(true);
            ammoHUD.gameObject.SetActive(false);
            hpHUD.gameObject.SetActive(false);
            mipmap.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(false);
            lobbyMenuUIContainer.SetActive(false);
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
        }

        /// <summary>
        /// This is called right after the team selection is done and the player is ready to spawn.
        /// </summary>
        public void InitializeSpawn(Team team)
        {
            dashboard.gameObject.SetActive(true);
            spawnCamera.gameObject.SetActive(false);
            teamSelector.SetActive(false);
            ammoHUD.gameObject.SetActive(true);
            hpHUD.gameObject.SetActive(true);
            mipmap.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(true);
            menuCamera.gameObject.SetActive(false);
            lobbyMenuUIContainer.SetActive(false);
            serverManager.RespawnServerRpc((int)team, new PlayerStats(username: lobbyManager.Username));
            loadingBar.SetActive(false);
            pauseMenu.SetActive(false);
        }
    }
}