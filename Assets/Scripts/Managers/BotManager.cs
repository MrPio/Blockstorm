using System;
using System.Collections.Generic;
using EasyButtons;
using Model;
using Network;
using Prefabs.Player;
using Unity.Netcode;
using UnityEngine;

namespace Managers
{
    public class BotManager : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        private List<Player> _bots = new();
        private SceneManager _sm;

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
        }

        [Button]
        public void SpawnBot(Team team=Team.Yellow)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            var bot = Instantiate(playerPrefab).GetComponent<Player>();
            _bots.Add(bot);
            var username = $"Bot{_bots.Count}";
            bot.gameObject.name = username;
            
            bot.IsBot.Value = true;
            bot.GetComponent<NetworkObject>().Spawn();
            bot.Spawn(team, new PlayerStats(username: username));
        }
    }
}