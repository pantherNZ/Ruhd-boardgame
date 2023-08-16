using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TileComponent : MonoBehaviour
{
    [SerializeField] private Side _rotation;
    public Side rotation
    {
        get => _rotation;
        set => SetRotation( value, false );
    }

    public TileData data;
    public bool flipped;
    private bool dragging;

    [HideInInspector] public Sprite backsideSprite;
    private Sprite storedSprite;
    private Side storedRotation;
    private Coroutine rotationInterp;

    public void SetInteractable( bool interactable )
    {
        GetComponent<Draggable>().enabled = interactable;
        GetComponent<EventDispatcherV2>().enabled = interactable;
    }

    public void SkipRotateInterpolation()
    {
        if( rotationInterp != null )
            StopCoroutine( rotationInterp );
        transform.localEulerAngles = new Vector3( 0.0f, 0.0f, ( float )_rotation * -90.0f );
        rotationInterp = null;
    }

    public void SetRotation( Side newRot, bool interpolate )
    {
        SkipRotateInterpolation();
        _rotation = newRot;
        if( interpolate )
            rotationInterp = StartCoroutine( InterpolateRotation() );
        else
            SkipRotateInterpolation();
    }
    
    IEnumerator InterpolateRotation()
    {
        var newRotation = ( float )_rotation * -90.0f - transform.localEulerAngles.z;
        newRotation = Utility.Mod( newRotation + 180, 360 ) - 180;
        yield return Utility.InterpolateRotation( transform, new Vector3( 0.0f, 0.0f, newRotation ), GameConstants.Instance.tileRotationInterpSec, true );
        rotationInterp = null;
    }

    public void ShowBack()
    {
        var image = GetComponent<Image>();
        storedSprite = image.sprite;
        image.sprite = backsideSprite;
        var rectTransform = transform as RectTransform;
        storedRotation = _rotation;
        rectTransform.localEulerAngles = new Vector3( 0.0f, 0.0f, 0.0f );
        flipped = true;
    }

    public void ShowFront( bool confirmed = true )
    {
        var image = GetComponent<Image>();
        image.sprite = storedSprite;
        rotation = storedRotation;
        SkipRotateInterpolation();

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
            if( rotationInterp != null && Mathf.Abs( Input.mouseScrollDelta.y ) > 0.001f )
            {
                SetRotation( ( Side )Utility.Mod( ( int )rotation + Mathf.RoundToInt( Mathf.Sign( Input.mouseScrollDelta.y ) ), 4 ), true );
            }
        }
    }
}
