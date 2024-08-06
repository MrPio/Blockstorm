using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
internal enum ActionType
{
    None,
    ToggleFullScreen,
    ExitGame
}

public class ClickableUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Image image;
    [SerializeField] private ActionType actionType;
    private Color startColor;

    private void Start()
    {
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
    }
}