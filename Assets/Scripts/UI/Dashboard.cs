using System.Collections;
using Managers;
using Network;
using TMPro;
using Unity.Netcode;
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

        private ServerManager _serverManager;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            _sm.dashboard.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _sm.dashboard.gameObject.SetActive(true);
                InvokeRepeating(nameof(DashboardLoop), 0f, refreshRate);
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                _sm.dashboard.gameObject.SetActive(false);
                CancelInvoke(nameof(DashboardLoop));
            }
        }

        private void DashboardLoop()
        {
            _serverManager.RequestPlayerListServerRpc();
        }

        public void UpdateDashboard(ulong[] playerIds)
        {
            foreach (Transform child in yellowStats)
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