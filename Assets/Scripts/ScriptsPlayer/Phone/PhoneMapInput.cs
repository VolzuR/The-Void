using UnityEngine;
using UnityEngine.EventSystems;

public class PhoneMapInput : MonoBehaviour, IDragHandler, IScrollHandler, IPointerClickHandler
{
    public System.Action<PointerEventData> Dragged, Scrolled, Clicked;

    public void OnDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left && Dragged != null) Dragged(e);
    }

    public void OnScroll(PointerEventData e)
    {
        if (Scrolled != null) Scrolled(e);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (Clicked != null) Clicked(e);
    }
}