using TMPro;
using Unity.Netcode;
using UnityEngine;
using Stack = Partials.Stack;

namespace UI
{
    public class Dashboard : MonoBehaviour
    {
        [SerializeField] private GameObject dashboard;
        [SerializeField] private GameObject playerStat;
        [SerializeField] private Transform blueStats, redStats, greenStats, yellowStats;
        [SerializeField] private float refreshRate = 1f;
        private float _acc;

        private void Start()
        {
            dashboard.SetActive(false);
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
                dashboard.SetActive(true);
            if (Input.GetKeyUp(KeyCode.Tab))
                dashboard.SetActive(false);

            if (Input.GetKey(KeyCode.Tab) && _acc > refreshRate)
            {
                _acc = 0;
                // TODO: only the server can access ConnectedClientsList, so a network variable or a Client RPC should be created
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    // var playerObject = client.PlayerObject; | TODO: use playerObject.name as players' username 
                    var stat = Instantiate(playerStat, yellowStats);
                    stat.name = client.ClientId.ToString();
                    stat.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text =
                        client.ClientId.ToString();
                }

                yellowStats.GetComponent<Stack>().UpdateUI();
            }

            _acc += Time.deltaTime;
            if (Input.GetKey(KeyCode.Tab) && _acc > refreshRate)
            {
                foreach (Transform child in yellowStats)
                    Destroy(child.gameObject);
            }
        }
    }
}