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
            ( transform as RectTransform ).localEulerAngles = new Vector3( 0.0f, 0.0f, ( float )_rotation * 90.0f );
        }
    }

    public CardData data;

    public void OnDragStart()
    {
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { card = this } );
    }

    public void OnDragEnd()
    {
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { card = this } );
    }
}
