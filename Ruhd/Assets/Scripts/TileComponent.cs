using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum TileSource
{
    Deck,
    Hand,
    Board,
}

public struct TileNetworkData : IEquatable<TileNetworkData>, INetworkSerializable
{
    public TileSource source;
    public bool flipped;
    public Vector2Int location;
    public Side rotation;

    public override bool Equals( object obj ) => obj is TileNetworkData other && Equals( other );
    public bool Equals( TileNetworkData other ) =>
            source == other.source
            && flipped == other.flipped
            && location == other.location
            && rotation == other.rotation;
    public override int GetHashCode() { return HashCode.Combine( ( int )source, flipped, location, ( int )rotation ); }
    public static bool operator ==( TileNetworkData lhs, TileNetworkData rhs ) => lhs.Equals( rhs );
    public static bool operator !=( TileNetworkData lhs, TileNetworkData rhs ) => !( lhs == rhs );
    void INetworkSerializable.NetworkSerialize<T>( BufferSerializer<T> serializer )
    {
        serializer.SerializeValue( ref source );
        serializer.SerializeValue( ref flipped );
        serializer.SerializeValue( ref location );
        serializer.SerializeValue( ref rotation );
    }
}

[Serializable]
public class TileComponent : MonoBehaviour
{
    [SerializeField] private Side _rotation;
    public Side rotation
    {
        get => _rotation;
        set => SetRotation( value, false );
    }

    [SerializeField] private bool _flipped;
    public bool flipped 
    { 
        get => _flipped; 
        set => _flipped = networkData.flipped = value; 
    }

    public TileNetworkData networkData = new TileNetworkData(){ source = TileSource.Deck };
    public TileData data;
    private bool dragging;

    [HideInInspector] public Sprite backsideSprite;
    private Sprite storedSprite;
    private Side storedRotation;
    private Coroutine rotationInterp;

    private PlayerController localPlayerController;
    private Draggable draggableCmp;

    private void Start()
    {
        draggableCmp = GetComponent<Draggable>();
    }

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
        networkData.rotation = newRot;
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

    private bool NetworkTurnCheck()
    {
        return !localPlayerController || localPlayerController.isPlayerTurn;
    }

    public void OnDragStart()
    {
        // Can only move when it's the local players turn
        if( !NetworkTurnCheck() )
            return;

        draggableCmp.StartDrag();
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { tile = this } );
        dragging = true;
    }

    public void OnDragEnd()
    {
        // Can only move when it's the local players turn
        if( !NetworkTurnCheck() )
            return;

        draggableCmp.EndDrag();
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { tile = this } );
        dragging = false;
    }

    private void Update()
    {
        if( localPlayerController == null && NetworkManager.Singleton && NetworkManager.Singleton.LocalClient != null )
            localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if( dragging )
        {
            if( NetworkTurnCheck() && rotationInterp == null && Mathf.Abs( Input.mouseScrollDelta.y ) > 0.001f )
            {
                SetRotation( ( Side )Utility.Mod( ( int )rotation + Mathf.RoundToInt( Mathf.Sign( Input.mouseScrollDelta.y ) ), 4 ), true );
            }
        }
    }

    // If inOpenHand is true, then position just uses the X for the index in the hand
    public void SetData( TileSource source, Vector2Int location )
    {
        networkData.source = source;
        networkData.location = location;
    }
}
