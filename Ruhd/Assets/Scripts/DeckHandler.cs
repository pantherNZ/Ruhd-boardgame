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
    private List<TileComponent> slots;
    private List<TileData> allTiles;
    private int? selectedCardFromSlot;

    protected override void Start()
    {
        base.Start();

        Reset();
    }

    public void Reset()
    {
        if( constants.rngSeed != 0 )
            Random.InitState( constants.rngSeed );

        slots = new List<TileComponent>();
        allTiles = new List<TileData>();
        foreach( var card in DataHandler.GetAllCards() )
            allTiles.Add( Instantiate( card ) );
        allTiles.RandomShuffle();

        for( int i = 0; i < constants.deckNumStartingCards; ++i )
            DrawCardToOpenHand();
    }

    public bool IsDeckEmpty()
    {
        return allTiles.IsEmpty();
    }

    public bool IsOpenHandEmpty()
    {
        return slots.IsEmpty();
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
        slots.Add( newCard );
        newCard.transform.SetParent( slotsPanelUI.transform, false );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TileSelectedEvent tileSelectedEvent )
        {
            foreach( var( idx, tile ) in slots.Enumerate() )
            {
                if( tileSelectedEvent.tile == tile )
                {
                    selectedCardFromSlot = idx;
                    break;
                }
            }
        }
        // Check we have a slot selected as this component is also used by the menu (and will receive events if still active)
        else if( e is TilePlacedEvent tilePlaced && !tilePlaced.tile.flipped && selectedCardFromSlot.HasValue )
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
                slots.RemoveAt( selectedCardFromSlot.Value );

                //If deck is empty then we need to draw more tiles
                if( IsOpenHandEmpty() )
                {
                    for( int i = 0; i < constants.deckNumStartingCards; ++i )
                        DrawCardToOpenHand();
                }
            }

            selectedCardFromSlot = null;
        }
    }
}
