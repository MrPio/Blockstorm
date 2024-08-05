using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickableUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Image image;
    private Color startColor;

    private void Start()
    {
        startColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        print("enter");
        image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        print("exit");
        image.color = startColor;
    }
}