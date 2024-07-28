using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class NetworkContainerUI : MonoBehaviour
    {
        [SerializeField] private Button hostBtn, serverBtn, clientBtn;

        private void Start()
        {
            hostBtn.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartHost();
                gameObject.SetActive(false);
            });
            serverBtn.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartServer();
                gameObject.SetActive(false);
            });
            clientBtn.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartClient();
                gameObject.SetActive(false);
            });
        }
    }
}