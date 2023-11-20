using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandler : EventReceiverInstance
{
    [SerializeField] GameConstants constants;
    [SerializeField] Canvas canvas;
    [SerializeField] HorizontalLayoutGroup slotsPanelUI;
    [SerializeField] TileComponent tilePrefab;
    [SerializeField] Sprite cardBackSprite;
    [SerializeField] TMPro.TextMeshProUGUI cardsLeftText;
    private List<TileComponent> openHand = new List<TileComponent>();
    private List<TileData> allTiles = new List<TileData>();
    private int? selectedCardFromSlot;
    private int numPlayers;
    private Coroutine textSpinRoutine;

    protected override void Start()
    {
        base.Start();
    }

    public void Reset( int numPlayers, Utility.IRandom rng )
    {
        this.numPlayers = numPlayers;
        openHand = new List<TileComponent>();
        allTiles = new List<TileData>();
        foreach( var card in DataHandler.GetAllCards() )
            allTiles.Add( Instantiate( card ) );
        allTiles.RandomShuffle( rng );
        allTiles.Resize( GetNumTotalCards() );

        for( int i = 0; i < GetNumStartingCards(); ++i )
            DrawCardToOpenHand( rng );

    }

    public bool IsDeckEmpty()
    {
        return allTiles.IsEmpty();
    }

    public bool IsOpenHandEmpty()
    {
        return openHand.IsEmpty();
    }

    public TileComponent FindTileInOpenHand( TileNetworkData tile )
    {
        return openHand.Find( x => x.networkData == tile );
    }

    public TileComponent DrawTile( Utility.IRandom rng )
    {
        var cardData = allTiles.PopBack();
        var newCard = Instantiate( tilePrefab );
        newCard.data = cardData;
        newCard.backsideSprite = cardBackSprite;
        newCard.GetComponent<Draggable>().AssignCanvas( canvas );

        var sprite = Resources.Load<Sprite>( cardData.imagePath );
        newCard.GetComponent<Image>().sprite = Instantiate( sprite );

        if( rng != null )
            newCard.rotation = Utility.GetEnumValues<Side>().RandomItem( rng: rng );

        if( cardsLeftText != null )
        {
            cardsLeftText.text = allTiles.Count.ToString();
            if( textSpinRoutine == null )
                textSpinRoutine = StartCoroutine( AnimateCardsLeftText() );
        }

        return newCard;
    }

    private IEnumerator AnimateCardsLeftText()
    {
        var spinTimeSec = 0.2f / 3.0f;
        var rot = new Vector3( 0.0f, 0.0f, -360.0f / 3.0f );
        yield return Utility.InterpolateRotation( cardsLeftText.transform, rot, spinTimeSec, true, Utility.Easing.Elastic.In );
        yield return Utility.InterpolateRotation( cardsLeftText.transform, rot, spinTimeSec, true, Utility.Easing.Linear );
        yield return Utility.InterpolateRotation( cardsLeftText.transform, rot, spinTimeSec, true, Utility.Easing.Linear );
        textSpinRoutine = null;
    }

    public void DrawCardToOpenHand( Utility.IRandom rng )
    {
        var newCard = DrawTile( rng );
        newCard.SetData( TileSource.Hand, new Vector2Int( openHand.Count, 0 ) );
        openHand.Add( newCard );
        newCard.transform.SetParent( slotsPanelUI.transform, false );
    }

    public ReadOnlyCollection<TileComponent> GetOpenHand()
    {
        return openHand.AsReadOnly();
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent startGameEvent )
        {
            Reset( startGameEvent.playerData.Count, GameController.Instance.gameRandom );
        }
        else if( e is TileSelectedEvent tileSelectedEvent )
        {
            selectedCardFromSlot = null;

            foreach( var( idx, tile ) in openHand.Enumerate() )
            {
                if( tileSelectedEvent.tile == tile )
                {
                    selectedCardFromSlot = idx;
                    break;
                }
            }
        }
        // Check we have a slot selected as this component is also used by the menu (and will receive events if still active)
        else if( e is TilePlacedEvent tilePlaced && selectedCardFromSlot.HasValue )
        {
            if( !tilePlaced.successfullyPlaced )
            {
                // + 1 to account for the placeholder deck card
                tilePlaced.tile.transform.SetSiblingIndex( selectedCardFromSlot.Value + 1 );
                var oldPosition = tilePlaced.tile.transform.localPosition;
                LayoutRebuilder.ForceRebuildLayoutImmediate( slotsPanelUI.transform as RectTransform );
                var newPosition = tilePlaced.tile.transform.localPosition;
                tilePlaced.tile.transform.localPosition = oldPosition;
                this.InterpolatePosition( tilePlaced.tile.transform, newPosition, 0.1f, true );
            }
            else
            {
                // Successfully placed
                openHand.RemoveAt( selectedCardFromSlot.Value );

                //If deck is empty then we need to draw more tiles
                if( IsOpenHandEmpty() )
                {
                    if( !allTiles.IsEmpty() )
                    {
                        for( int i = 0; i < GetNumStartingCards(); ++i )
                            DrawCardToOpenHand( GameController.Instance.gameRandom );
                    }
                    else
                    {
                        EventSystem.Instance.TriggerEvent( new GameOverEvent() );
                    }
                }

                //EventSystem.Instance.TriggerEvent( new GameOverEvent() );
            }

            selectedCardFromSlot = null;
        }
    }

    int GetNumStartingCards()
    {
        return slotsPanelUI == null ? 0 : Mathf.Clamp( numPlayers + 1, 2, 4 );
    }

    int GetNumTotalCards()
    {
        return slotsPanelUI == null ? allTiles.Count : ( 4 + Mathf.Clamp( numPlayers * 6, 0, allTiles.Count ) );
    }
}
