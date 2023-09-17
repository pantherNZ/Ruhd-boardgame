using UnityEngine;
using UnityEngine.EventSystems;

public class TileHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float highlightScale = 1.2f;
    [SerializeField] float highlightSpeed = 0.1f;
    [SerializeField] Utility.EasingFunctionMethod easingMethod;
    [SerializeField] Utility.EasingFunctionTypes easingType;

    void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
    {
        Hover();
    }

    public void Hover()
    {
        this.InterpolateScale( new Vector3( highlightScale, highlightScale, highlightScale ), highlightSpeed, Utility.FetchEasingFunction( easingType, easingMethod ) );
    }

    void IPointerExitHandler.OnPointerExit( PointerEventData eventData )
    {
        Unhover();
    }

    public void Unhover()
    {
        this.InterpolateScale( Vector3.one, highlightSpeed, Utility.FetchEasingFunction( easingType, easingMethod ) );
    }
}