using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AshVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static Vector2 InputVector { get; private set; }
    RectTransform area;
    RectTransform knob;
    void Awake(){area=GetComponent<RectTransform>();var child=transform.Find("Knob");if(child!=null)knob=child.GetComponent<RectTransform>();}
    public void OnPointerDown(PointerEventData e){OnDrag(e);}
    public void OnDrag(PointerEventData e)
    {
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out var p))return;
        Vector2 half=area.rect.size*.5f;
        InputVector=Vector2.ClampMagnitude(new Vector2(p.x/Mathf.Max(half.x,1),p.y/Mathf.Max(half.y,1)),1f);
        if(knob!=null)knob.anchoredPosition=InputVector*half*.48f;
    }
    public void OnPointerUp(PointerEventData e){InputVector=Vector2.zero;if(knob!=null)knob.anchoredPosition=Vector2.zero;}
}
