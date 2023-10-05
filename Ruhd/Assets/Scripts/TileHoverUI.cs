using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class TileHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float highlightScale = 1.2f;
    [SerializeField] float highlightSpeed = 0.1f;
    [SerializeField] Utility.EasingFunctionMethod easingMethod;
    [SerializeField] Utility.EasingFunctionTypes easingType;
    public Func<bool> canHoverCheck;

    void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
    {
        if( canHoverCheck == null || canHoverCheck() )
            Hover();
    }

    public void Hover()
    {
        if( canHoverCheck == null || canHoverCheck() )
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