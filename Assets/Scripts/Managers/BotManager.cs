using System;
using System.Collections.Generic;
using System.Linq;
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
            bot.IsBot.Value = true; // Right now, this order is mandadory
            bot.GetComponent<NetworkObject>().Spawn();

            // Load random Equipment
            var status = bot.Status.Value;
            status.Melee = Weapon.Melees.RandomItem();
            status.Primary = Weapon.Primaries.RandomItem();
            status.Secondary = Weapon.Secondaries.RandomItem();
            status.Tertiary = Weapon.Tertiaries.Where(it=>it.Name!="tact").ToList().RandomItem();
            status.Grenade = Weapon.Grenades.RandomItem();
            status.GrenadeSecondary = Weapon.GrenadesSecondary.RandomItem();

            bot.Spawn(
                newTeam: team,
                playerStats: new PlayerStats(username: username),
                playerStatus: status,
                isBot: true
            );
        }
    }
}