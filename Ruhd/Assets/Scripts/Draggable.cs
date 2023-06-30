using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class Draggable : MonoBehaviour
{
    bool dragging;
    Vector2 offset, startPos;

    new RectTransform transform;
    public Action<Draggable, Vector3> updatePosition;
    // Return value decides if the new position should be saved or not (otherwise it resets to start of the drag)
    public Func<Draggable, Vector3, bool> onDragEnd;

    void Start()
    {
        transform = base.transform as RectTransform;
    }

    public void StartDrag()
    {
        if( dragging || !enabled )
            return;

        dragging = true;
        startPos = transform.anchoredPosition;
        var targetPos = Utility.GetMouseOrTouchPos();
        offset = transform.anchoredPosition - new Vector2( targetPos.x, targetPos.y );
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
            var targetPos = Utility.GetMouseOrTouchPos() + offset;
            if( updatePosition != null )
                updatePosition.Invoke( this, targetPos );
            else
                transform.anchoredPosition = targetPos;
            transform.SetAsLastSibling();
        }
    }
}