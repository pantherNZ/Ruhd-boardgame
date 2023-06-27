using System;
using UnityEngine;

[Serializable]
public class CardComponent : MonoBehaviour
{
    [SerializeField] private Side _rotation;
    public Side rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            ( transform as RectTransform ).localEulerAngles = new Vector3( 0.0f, 0.0f, ( float )_rotation * -90.0f );
        }
    }

    public CardData data;
    private bool dragging;

    public void OnDragStart()
    {
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { card = this } );
        dragging = true;
    }

    public void OnDragEnd()
    {
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { card = this } );
        dragging = false;
    }

    private void Update()
    {
        if( dragging )
        {
            if( Mathf.Abs( Input.mouseScrollDelta.y ) > 0.001f )
            {
                rotation = ( Side )Utility.Mod( ( int )rotation + Mathf.RoundToInt( Mathf.Sign( Input.mouseScrollDelta.y ) ), 4 );
            }
        }
    }
}
