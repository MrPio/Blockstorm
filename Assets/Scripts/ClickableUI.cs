using System;
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
    ConfirmEditText
}

public class ClickableUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Image image;
    [SerializeField] private ActionType actionType;

    [Header("EditText")] [SerializeField] private GameObject text;
    [SerializeField] private GameObject editText;
    [SerializeField] private GameObject confirmIcon;

    [Header("ConfirmEditText")] [SerializeField]
    private TextMeshProUGUI editTextText;

    [SerializeField] private UsernameUI usernameUI;

    private Color startColor;

    private void Start()
    {
        if (image is not null)
            startColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image is not null)
            image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image is not null)
            image.color = startColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (actionType is ActionType.ToggleFullScreen)
            Screen.fullScreenMode = Screen.fullScreenMode is not FullScreenMode.ExclusiveFullScreen
                ? FullScreenMode.ExclusiveFullScreen
                : FullScreenMode.Windowed;
        if (actionType is ActionType.ExitGame)
            Application.Quit(0);
        if (actionType is ActionType.EditText)
        {
            text.gameObject.SetActive(!text.gameObject.activeSelf);
            editText.gameObject.SetActive(!text.gameObject.activeSelf);
            confirmIcon?.gameObject.SetActive(!text.gameObject.activeSelf);
        }

        if (actionType is ActionType.ConfirmEditText)
            usernameUI.SaveUsername(editTextText.text);
    }
}