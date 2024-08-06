using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Network;
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
            if (isTeamSelector)
            {
                InvokeRepeating(nameof(DashboardLoop), 0.5f, refreshRate);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    transform.GetChild(0).gameObject.SetActive(true);
                    InvokeRepeating(nameof(DashboardLoop), 0f, refreshRate);
                }

                if (Input.GetKeyUp(KeyCode.Tab))
                {
                    transform.GetChild(0).gameObject.SetActive(false);
                    CancelInvoke(nameof(DashboardLoop));
                }
            }
        }

        private void DashboardLoop() =>
            _sm.serverManager.RequestPlayerListServerRpc();

        public void UpdateDashboard(ulong[] playerIds)
        {
            // Prevent useless update // TODO: also take Kill&Death into consideration!
            if (playerIds.Length == _lastPlayerIds.Count && playerIds.All(it => _lastPlayerIds.Contains(it)))
                return;
            _lastPlayerIds = playerIds.ToList();
            foreach (Transform child in yellowStats)
                if (child is not null && !child.IsDestroyed())
                    Destroy(child.gameObject);

            StartCoroutine(AddPlayers());
            return;

            IEnumerator AddPlayers()
            {
                // Wait for the next frame to ensure that the garbage collector has destroyed the last game objects
                yield return null;
                foreach (var client in playerIds)
                {
                    // var playerObject = client.PlayerObject; | TODO: use playerObject.name as players' username 
                    var stat = Instantiate(playerStat, yellowStats);
                    stat.name = client.ToString();
                    stat.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text =
                        client.ToString();
                }

                yellowStats.GetComponent<Stack>().UpdateUI();
            }
        }
    }
}