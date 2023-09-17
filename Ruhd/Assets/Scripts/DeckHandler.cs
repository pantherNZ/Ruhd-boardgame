using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandler : EventReceiverInstance
{
    [SerializeField] GameConstants constants;
    [SerializeField] Canvas canvas;
    [SerializeField] HorizontalLayoutGroup slotsPanelUI;
    [SerializeField] TileComponent tilePrefab;
    [SerializeField] Sprite cardBackSprite;
    private List<TileComponent> openHand = new List<TileComponent>();
    private List<TileData> allTiles = new List<TileData>();
    private int? selectedCardFromSlot;
    private int numPlayers;

    protected override void Start()
    {
        base.Start();
    }

    public void Reset( int numPlayers )
    {
        this.numPlayers = numPlayers;
        openHand = new List<TileComponent>();
        allTiles = new List<TileData>();
        foreach( var card in DataHandler.GetAllCards() )
            allTiles.Add( Instantiate( card ) );
        allTiles.RandomShuffle();

        for( int i = 0; i < GetNumStartingCards(); ++i )
            DrawCardToOpenHand();
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

    public TileComponent DrawTile( bool randomRotation )
    {
        var cardData = allTiles.PopBack();
        var newCard = Instantiate( tilePrefab );
        newCard.data = cardData;
        newCard.backsideSprite = cardBackSprite;
        newCard.GetComponent<Draggable>().AssignCanvas( canvas );

        var sprite = Resources.Load<Sprite>( cardData.imagePath );
        newCard.GetComponent<Image>().sprite = Instantiate( sprite );

        if( randomRotation )
            newCard.rotation = Utility.GetEnumValues<Side>().RandomItem();

        return newCard;
    }

    public void DrawCardToOpenHand()
    {
        var newCard = DrawTile( true );
        newCard.SetData( TileSource.Hand, new Vector2Int( openHand.Count, 0 ) );
        openHand.Add( newCard );
        newCard.transform.SetParent( slotsPanelUI.transform, false );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent startGameEvent )
        {
            Reset( startGameEvent.playerData.Count );
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
                LayoutRebuilder.ForceRebuildLayoutImmediate( slotsPanelUI.transform as RectTransform );
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
                            DrawCardToOpenHand();
                    }
                    else
                    {
                        EventSystem.Instance.TriggerEvent( new GameOverEvent() );
                    }
                }

                EventSystem.Instance.TriggerEvent( new GameOverEvent() );
            }

            selectedCardFromSlot = null;
        }
    }

    int GetNumStartingCards()
    {
        return slotsPanelUI == null ? 0 : Mathf.Max( 2, numPlayers );
    }
}
