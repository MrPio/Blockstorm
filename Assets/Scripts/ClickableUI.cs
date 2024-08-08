using System;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    CloseMessageBox
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
    private TextMeshProUGUI joinSessionCodeText;

    [SerializeField] private TextMeshProUGUI joinSessionPasswordText;

    [Header("JoinPrivateLobbyMessagebox")] [SerializeField]
    private GameObject joinPrivateLobbyMessagebox;

    [Header("NewLobby")] [SerializeField] private TextMeshProUGUI newLobbyMap;
    [SerializeField] private TextMeshProUGUI newLobbyPassword;

    [Header("NewLobbyMessagebox")] [SerializeField]
    private GameObject newLobbyMessagebox;

    [NonSerialized] public string LobbyCode;
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
            var success = await _sm.lobbyManager.JoinLobby(LobbyCode);
            if (success)
                _sm.InitializeMatch();
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
            var success = await _sm.lobbyManager.JoinLobby(joinSessionCodeText.text,
                password: joinSessionPasswordText.text.Length <= 0 ? null : joinSessionPasswordText.text);
            if (success)
            {
                Destroy(transform.parent.parent.gameObject);
                _sm.InitializeMatch();
            }
        }

        if (actionType is ActionType.JoinPrivateLobbyMessagebox)
            Instantiate(joinPrivateLobbyMessagebox, _sm.uiCanvas.transform);

        if (actionType is ActionType.NewLobby)
        {
            var relayCode = await _sm.relayManager.CreateRelay();
            var success = await _sm.lobbyManager.CreateLobby(newLobbyMap.text, relayCode,
                password: newLobbyPassword.text.Length <= 0 ? null : newLobbyPassword.text,
                map: newLobbyMap.text);
            if (success)
            {
                Destroy(transform.parent.parent.gameObject);
                _sm.InitializeMatch();
            }
        }

        if (actionType is ActionType.NewLobbyMessagebox)
            Instantiate(newLobbyMessagebox, _sm.uiCanvas.transform);
    }
}