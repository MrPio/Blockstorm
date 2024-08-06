using Managers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class NetworkContainerUI : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private Button hostBtn, clientBtn;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            hostBtn?.onClick.AddListener(() => StartGame(isHost: true));
            clientBtn?.onClick.AddListener(() => StartGame(isHost: false));
        }

        private async void StartGame(bool isHost)
        {
            // if (isHost)
            // {
            //     var code = await _sm.relayManager.CreateRelay();
            //     _sm.lobbyManager.UpdateHostedLobby();
            // }
            // else
            //     _sm.relayManager.JoinRelay();
            gameObject.SetActive(false);
            _sm.InitializeMatch();
        }
    }
}