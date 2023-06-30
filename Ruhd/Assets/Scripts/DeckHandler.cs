using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandler : EventReceiverInstance
{
    [SerializeField] GameConstants constants;
    [SerializeField] HorizontalLayoutGroup slotsPanelUI;
    [SerializeField] TileComponent tilePrefab;
    [SerializeField] Sprite cardBackSprite;
    private List<TileComponent> slots;
    private List<TileData> allCards;
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
        allCards = new List<TileData>();
        foreach( var card in DataHandler.GetAllCards() )
            allCards.Add( Instantiate( card ) );
        allCards.RandomShuffle();

        for( int i = 0; i < constants.deckNumStartingCards; ++i )
            DrawCardToOpenHand();
    }

    public bool IsDeckEmpty()
    {
        return allCards.IsEmpty();
    }

    public bool IsOpenHandEmpty()
    {
        return slots.IsEmpty();
    }

    public TileComponent DrawTile( bool randomRotation )
    {
        var cardData = allCards.PopBack();
        var newCard = Instantiate( tilePrefab );
        newCard.data = cardData;
        newCard.backsideSprite = cardBackSprite;

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
        newCard.transform.SetParent( slotsPanelUI.transform );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TileSelectedEvent tileSelectedEvent )
        {
            foreach( var( idx, card ) in slots.Enumerate() )
            {
                if( tileSelectedEvent.tile == card )
                {
                    selectedCardFromSlot = idx;
                    break;
                }
            }
        }
        else if( e is TilePlacedEvent tilePlaced && !tilePlaced.tile.flipped )
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
