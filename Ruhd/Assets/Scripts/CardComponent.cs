using System;
using UnityEngine;

[Serializable]
public class CardComponent : MonoBehaviour
{
    private Side _rotation;
    public Side rotation
    {
        get => _rotation;
        set
        {
            ( transform as RectTransform ).localEulerAngles = new Vector3( 0.0f, 0.0f, ( float )_rotation * 90.0f );
            _rotation = value;
        }
    }

    [HideInInspector] public CardData data;

    private void Start()
    {
    }

    public void OnDragStart()
    {
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { card = this } );
    }

    public void OnDragEnd()
    {
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { card = this } );
    }
}
