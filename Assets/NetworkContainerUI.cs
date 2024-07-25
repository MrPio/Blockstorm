using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkContainerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn, serverBtn, clientBtn;

    private void Awake()
    {
        hostBtn.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        serverBtn.onClick.AddListener(() => NetworkManager.Singleton.StartServer());
        clientBtn.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }
}