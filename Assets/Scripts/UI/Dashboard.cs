using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Network;
using Prefabs.Player;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Stack = Partials.Stack;

namespace UI
{
    public class Dashboard : MonoBehaviour
    {
        private SceneManager _sm;

        [SerializeField] private GameObject playerStat;
        [SerializeField] private Transform blueStats, redStats, greenStats, yellowStats;
        [SerializeField] private float refreshRate = 1f;
        [SerializeField] private bool isTeamSelector;
        private List<ulong> _lastPlayerIds = new();
        private bool _isLooping;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            if (!isTeamSelector)
                transform.GetChild(0).gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
                return;
            if (isTeamSelector && !_isLooping)
            {
                _isLooping = true;
                InvokeRepeating(nameof(UpdateDashboard), 0.5f, refreshRate);
            }
            else if (!isTeamSelector)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    transform.GetChild(0).gameObject.SetActive(true);
                    InvokeRepeating(nameof(UpdateDashboard), 0.05f, refreshRate);
                }

                if (Input.GetKeyUp(KeyCode.Tab))
                {
                    transform.GetChild(0).gameObject.SetActive(false);
                    CancelInvoke(nameof(UpdateDashboard));
                }
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(UpdateDashboard));
        }

        /*
        private void DashboardLoop() =>
            _sm.serverManager.RequestPlayerListServerRpc();
            */

        public void UpdateDashboard()
        {
            // Prevent useless update
            // if (playerIds.Length == _lastPlayerIds.Count && playerIds.All(it => _lastPlayerIds.Contains(it)))
            //     return;
            // _lastPlayerIds = playerIds.ToList();
            foreach (Transform child in redStats)
                if (child is not null && !child.IsDestroyed())
                    Destroy(child.gameObject);
            foreach (Transform child in blueStats)
                if (child is not null && !child.IsDestroyed())
                    Destroy(child.gameObject);
            foreach (Transform child in greenStats)
                if (child is not null && !child.IsDestroyed())
                    Destroy(child.gameObject);
            foreach (Transform child in yellowStats)
                if (child is not null && !child.IsDestroyed())
                    Destroy(child.gameObject);

            StartCoroutine(AddPlayers());
            return;

            IEnumerator AddPlayers()
            {
                // Wait for the next frame to ensure that the garbage collector has destroyed the last game objects
                yield return null;
                var players = FindObjectsOfType<Player>();
                foreach (var player in players)
                {
                    var stat = Instantiate(playerStat,
                        player.Team switch
                        {
                            Team.Red => redStats,
                            Team.Blue => blueStats,
                            Team.Green => greenStats,
                            _ => yellowStats
                        });
                    stat.name = $"{player.Stats.Value.Username.Value} ({player.OwnerClientId})";
                    stat.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = stat.name;
                    stat.transform.Find("KillsText").GetComponent<TextMeshProUGUI>().text =
                        player.Stats.Value.Kills.ToString();
                    stat.transform.Find("DeathsText").GetComponent<TextMeshProUGUI>().text =
                        player.Stats.Value.Deaths.ToString();
                }

                yellowStats.GetComponent<Stack>().UpdateUI();
            }
        }
    }
}