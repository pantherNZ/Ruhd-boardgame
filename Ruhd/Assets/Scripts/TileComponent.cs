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
    public Vector2Int location;
    public Side rotation;

    public override bool Equals( object obj ) => obj is TileNetworkData other && Equals( other );
    public bool Equals( TileNetworkData other ) =>
            source == other.source
            && location == other.location
            && rotation == other.rotation;
    public override int GetHashCode() { return HashCode.Combine( ( int )source, location, ( int )rotation ); }
    public static bool operator ==( TileNetworkData lhs, TileNetworkData rhs ) => lhs.Equals( rhs );
    public static bool operator !=( TileNetworkData lhs, TileNetworkData rhs ) => !( lhs == rhs );
    void INetworkSerializable.NetworkSerialize<T>( BufferSerializer<T> serializer )
    {
        serializer.SerializeValue( ref source );
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

    public TileNetworkData networkData = new TileNetworkData(){ source = TileSource.Deck };
    public TileData data;
    public bool draggable = true;
    private bool dragging;
    private bool interactable = true;
    private string challenger;

    [HideInInspector] public Sprite backsideSprite;
    [SerializeField] GameObject ghostedSpritePrefab;
    private Sprite storedSprite;
    private Side storedRotation;
    private Coroutine rotationInterp;

    private PlayerController localPlayerController;
    private Draggable draggableCmp;
    private TileHoverUI hoverCmp;
    private GameObject ghostedSprite;

    private void Awake()
    {
        draggableCmp = GetComponent<Draggable>();
        hoverCmp = GetComponent<TileHoverUI>();
    }

    private void Start()
    {
        if( data != null )
            data.owningComponent = this;

        GetComponent<TileHoverUI>().canHoverCheck = TurnCheck;
    }

    public void SetInteractable( bool interactable )
    {
        this.interactable = interactable;
        draggable = interactable;
        GetComponent<EventDispatcherV2>().enabled = interactable;
        draggableCmp.enabled = interactable;
        hoverCmp.enabled = interactable;
    }

    public void SetGhosted( bool ghosted )
    {
        if( ghosted )
        {
            ghostedSprite = Instantiate( ghostedSpritePrefab, transform );
            ghostedSprite.transform.localPosition = Vector3.zero;
        } 
        else if( ghostedSprite != null ) {
            ghostedSprite.Destroy();
        }

        var image = GetComponent<Image>();
        image.color = new Color( 1.0f, 1.0f, 1.0f, ghosted ? 0.5f : 1.0f );
    }

    public bool IsGhosted()
    {
        return ghostedSprite != null;
    }

    public void SetBeingChallenged( string challenger )
    {
        this.challenger = challenger;
    }

    public void SkipRotateInterpolation()
    {
        if( rotationInterp != null )
            StopCoroutine( rotationInterp );
        transform.localEulerAngles = new Vector3( 0.0f, 0.0f, _rotation.Value() * -90.0f );
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
        var newRotation = _rotation.Value() * -90.0f - transform.localEulerAngles.z;
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
    }

    public void ShowFront( bool confirmed = true )
    {
        var image = GetComponent<Image>();
        image.sprite = storedSprite;
        rotation = storedRotation;
        SkipRotateInterpolation();
    }

    private bool TurnCheck()
    {
        return GameController.Instance.isLocalPlayerTurn || GameController.Instance.isLocalPlayerChallenging;
    }

    public void OnDragStart()
    {
        // Can only move when it's the local players turn
        if( !TurnCheck() || !draggable )
            return;

        draggableCmp.StartDrag();
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { tile = this, showHighlights = true } );
        dragging = true;
    }

    public void OnDragEnd()
    {
        // Can only move when it's the local players turn
        if( !TurnCheck() || !draggable )
            return;

        hoverCmp.Unhover();
        draggableCmp.EndDrag();
        EventSystem.Instance.TriggerEvent( new TileDroppedEvent() { tile = this } );
        dragging = false;
    }

    private void Update()
    {
        if( !draggableCmp )
            return;

        if( localPlayerController == null && NetworkManager.Singleton && NetworkManager.Singleton.LocalClient != null )
            localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if( dragging )
        {
            if( TurnCheck() && rotationInterp == null && Mathf.Abs( Input.mouseScrollDelta.y ) > 0.001f )
            {
                SetRotation( ( Side )Utility.Mod( ( int )rotation + Mathf.RoundToInt( Mathf.Sign( Input.mouseScrollDelta.y ) ), 4 ), true );
            }
        }

        hoverCmp.enabled = interactable && TurnCheck();
    }

    // If inOpenHand is true, then position just uses the X for the index in the hand
    public void SetData( TileSource source, Vector2Int location )
    {
        networkData.source = source;
        networkData.location = location;
    }
}
