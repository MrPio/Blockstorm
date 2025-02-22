using System;
using System.Collections.Generic;
using EasyButtons;
using ExtensionFunctions;
using Model;
using Network;
using Prefabs.Player;
using Unity.Netcode;
using UnityEngine;
using Weapon = Model.Weapon;

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
        public void SpawnBot(Team team)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            var bot = Instantiate(playerPrefab).GetComponent<Player>();
            _bots.Add(bot);
            var username = $"Bot{_bots.Count}";
            bot.gameObject.name = username;

            // Load random Equipment
            var newStatus = bot.Status.Value;
            newStatus.Melee = Weapon.Melees.RandomItem();
            newStatus.Primary = Weapon.Primaries.RandomItem();
            newStatus.Secondary = Weapon.Secondaries.RandomItem();
            newStatus.Tertiary = Weapon.Tertiaries.RandomItem();
            newStatus.Grenade = Weapon.Grenades.RandomItem();
            newStatus.GrenadeSecondary = Weapon.GrenadesSecondary.RandomItem();
            bot.Status.Value = newStatus;

            bot.IsBot.Value = true;
            bot.GetComponent<NetworkObject>().Spawn();
            bot.Spawn(team, new PlayerStats(username: username));
        }
    }
}