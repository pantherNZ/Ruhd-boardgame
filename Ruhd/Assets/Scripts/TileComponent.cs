using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TileComponent : MonoBehaviour
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

    public TileData data;
    public bool flipped;
    private bool dragging;

    [HideInInspector] public Sprite backsideSprite;
    private Sprite storedSprite;
    private float storedRotation;

    public void SetInteractable( bool interactable )
    {
        GetComponent<Draggable>().enabled = interactable;
        GetComponent<EventDispatcherV2>().enabled = interactable;
    }

    public void ShowBack()
    {
        var image = GetComponent<Image>();
        storedSprite = image.sprite;
        image.sprite = backsideSprite;
        var rectTransform = transform as RectTransform;
        storedRotation = rectTransform.localEulerAngles.z;
        rectTransform.localEulerAngles = new Vector3( 0.0f, 0.0f, 0.0f );
        flipped = true;
    }

    public void ShowFront( bool confirmed = true )
    {
        var image = GetComponent<Image>();
        image.sprite = storedSprite;
        ( transform as RectTransform ).localEulerAngles = new Vector3( 0.0f, 0.0f, storedRotation );

        if( confirmed )
            flipped = false;
    }

    public void OnDragStart()
    {
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { tile = this } );
        dragging = true;
    }

    public void OnDragEnd()
    {
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { tile = this } );
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
