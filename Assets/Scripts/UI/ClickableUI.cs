using System;
using System.Linq;
using Managers;
using Model;
using Prefabs.Player;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelEngine;

namespace UI
{
    [Serializable]
    internal enum ActionType
    {
        None,
        ToggleFullScreen,
        ExitGame,
        EditText,
        ConfirmEditText,
        JoinLobby,
        JoinPrivateLobby,
        JoinPrivateLobbyMessagebox,
        NewLobby,
        NewLobbyMessagebox,
        CloseMessageBox,
        SelectTeam,
        QuitLobby,
        InventorySpawn
    }

    public class ClickableUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private SceneManager _sm;
        [SerializeField] private Color hoverColor;
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private ActionType actionType;

        [Header("EditText")] [SerializeField] private GameObject editText;
        [SerializeField] private GameObject confirmIcon;

        [Header("ConfirmEditText")] [SerializeField]
        private TextMeshProUGUI editTextText;

        [SerializeField] private UsernameUI usernameUI;

        [Header("JoinPrivateLobby")] [SerializeField]
        private TMP_InputField joinSessionCodeText;

        [SerializeField] private TMP_InputField joinSessionPasswordText;

        [Header("JoinPrivateLobbyMessagebox")] [SerializeField]
        private GameObject joinPrivateLobbyMessagebox;

        [Header("NewLobby")] [SerializeField] private TextMeshProUGUI newLobbyMap;
        [SerializeField] private TMP_InputField newLobbyPassword;

        [Header("NewLobbyMessagebox")] [SerializeField]
        private GameObject newLobbyMessagebox;

        [Header("SelectTeam")] [SerializeField]
        private Team team;

        [NonSerialized] public string LobbyId;
        private Color startColor;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            if (image is not null)
                startColor = image.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (image is not null)
                image.color = hoverColor;
            if (text is not null)
                text.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (image is not null)
                image.color = startColor;
            if (text is not null)
                text.color = startColor;
        }

        public async void OnPointerClick(PointerEventData eventData)
        {
            if (actionType is ActionType.ToggleFullScreen)
                Screen.fullScreenMode = Screen.fullScreenMode is not FullScreenMode.ExclusiveFullScreen
                    ? FullScreenMode.ExclusiveFullScreen
                    : FullScreenMode.Windowed;
            if (actionType is ActionType.ExitGame)
                Application.Quit(0);
            if (actionType is ActionType.JoinLobby)
            {
                _sm.InitializeLoading();
                await _sm.lobbyManager.JoinLobbyById(LobbyId);
            }

            if (actionType is ActionType.CloseMessageBox)
                Destroy(transform.parent.parent.gameObject);
            if (actionType is ActionType.EditText)
            {
                text.gameObject.SetActive(!text.gameObject.activeSelf);
                editText.gameObject.SetActive(!text.gameObject.activeSelf);
                confirmIcon?.gameObject.SetActive(!text.gameObject.activeSelf);
            }

            if (actionType is ActionType.ConfirmEditText)
                usernameUI.SaveUsername(editTextText.text);
            if (actionType is ActionType.JoinPrivateLobby && joinSessionCodeText.text.Length > 3)
            {
                Destroy(transform.parent.parent.gameObject);
                _sm.InitializeLoading();
                await _sm.lobbyManager.JoinLobbyByCode(joinSessionCodeText.text,
                    password: joinSessionPasswordText.text.Length <= 0 ? null : joinSessionPasswordText.text);
            }

            if (actionType is ActionType.JoinPrivateLobbyMessagebox)
                Instantiate(joinPrivateLobbyMessagebox, _sm.uiCanvas.transform);

            if (actionType is ActionType.NewLobby)
            {
                Destroy(transform.parent.parent.gameObject);
                _sm.InitializeLoading();
                await _sm.lobbyManager.CreateLobby(newLobbyMap.text,
                    map: newLobbyMap.text, password: newLobbyPassword.text.Length <= 0 ? null : newLobbyPassword.text);
            }

            if (actionType is ActionType.NewLobbyMessagebox)
                Instantiate(newLobbyMessagebox, _sm.uiCanvas.transform);

            if (actionType is ActionType.SelectTeam)
            {
                if (FindObjectsOfType<Player>().Select(it => it.Team == team).Count() < LobbyManager.MaxPlayers / 4)
                    _sm.InitializeInventory(team);
            }

            if (actionType is ActionType.QuitLobby)
            {
                NetworkManager.Singleton.Shutdown();
                await _sm.lobbyManager.LeaveLobby();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().buildIndex);
            }

            if (actionType is ActionType.InventorySpawn)
            {
                var inventory = FindObjectOfType<Inventory>();
                _sm.InitializeSpawn(selectedWeapons:inventory.selectedWeapons);
            }
        }
    }
}