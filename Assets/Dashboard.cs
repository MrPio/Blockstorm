using System.Collections;
using ExtensionFunctions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Stack = Partials.Stack;

public class Dashboard : MonoBehaviour
{
    [SerializeField] private GameObject dashboard;
    [SerializeField] private GameObject playerStat;
    [SerializeField] private Transform blueStats, redStats, greenStats, yellowStats;
    [SerializeField] private float refreshRate = 1f;
    private float _acc;

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
            foreach (Transform child in yellowStats)
                Destroy(child.gameObject);
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // var playerObject = client.PlayerObject; | TODO: use playerObject.name as players' username 
                Instantiate(playerStat, yellowStats).Apply(o =>
                {
                    o.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text =
                        client.ClientId.ToString();
                });
                Instantiate(playerStat, yellowStats).Apply(o =>
                {
                    o.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text =
                        client.ClientId.ToString();
                });
            }
            yellowStats.GetComponent<Stack>().UpdateUI();
        }
        _acc += Time.deltaTime;
    }
}