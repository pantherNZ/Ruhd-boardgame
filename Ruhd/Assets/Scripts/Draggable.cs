using UnityEngine;
using System;

public class Draggable : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    private bool dragging;
    private Vector2 offset, startPos;
    private Camera mainCam;

    new RectTransform transform;
    public Action<Draggable, Vector3> updatePosition;
    // Return value decides if the new position should be saved or not (otherwise it resets to start of the drag)
    public Func<Draggable, Vector3, bool> onDragEnd;

    void Start()
    {
        transform = base.transform as RectTransform;
        mainCam = Camera.main;
    }

    public void AssignCanvas( Canvas canvas )
    {
        this.canvas = canvas;
    }

    public Vector2 GetMousePos()
    {
        if( canvas != null )
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle( canvas.transform as RectTransform, Utility.GetMouseOrTouchPos(), mainCam, out var localPos );
            return localPos;
        }

        return Utility.GetMouseOrTouchPos();
    }

    public void StartDrag()
    {
        if( dragging || !enabled )
            return;

        dragging = true;
        startPos = transform.anchoredPosition;
        offset = transform.anchoredPosition - GetMousePos();
    }

    public void EndDrag()
    {
        if( !dragging || !enabled )
            return;
        dragging = false;

        if( onDragEnd != null && onDragEnd( this, transform.anchoredPosition ) == false )
        {
            transform.anchoredPosition = startPos;
        }
    }

    public void ResetOffset()
    {
        transform.anchoredPosition = Vector2.zero;
        transform.ForceUpdateRectTransforms();
        RectTransformUtility.ScreenPointToLocalPointInRectangle( canvas.transform as RectTransform, RectTransformUtility.WorldToScreenPoint( mainCam, transform.position ), mainCam, out var localPos );
        offset = -localPos;
    }

    public void ResetBackToDragStart()
    {
        transform.anchoredPosition = startPos;
    }

    public Vector2 GetDragStartPosition()
    {
        return startPos;
    }

    public bool IsDragging()
    {
        return dragging;
    }

    private void Update()
    {
        if( dragging )
        {
            var targetPos = GetMousePos() + offset;
            if( updatePosition != null )
                updatePosition.Invoke( this, targetPos );
            else
                transform.anchoredPosition = targetPos;
            transform.SetAsLastSibling();
        }
    }
}