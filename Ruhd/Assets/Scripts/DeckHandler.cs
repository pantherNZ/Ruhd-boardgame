using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandler : EventReceiverInstance
{
    [SerializeField] GameConstants constants;
    [SerializeField] HorizontalLayoutGroup slotsPanelUI;
    [SerializeField] CardComponent tilePrefab;
    private List<CardComponent> slots;
    private List<CardData> allCards;
    private int? selectedCardFromSlot;

    protected override void Start()
    {
        base.Start();

        if( constants.rngSeed != 0 )
            Random.InitState( constants.rngSeed );

       slots = new List<CardComponent>();
        allCards = new List<CardData>();
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

    public CardComponent DrawCard( bool randomRotation )
    {
        var cardData = allCards.PopBack();
        var newCard = Instantiate( tilePrefab );
        newCard.data = cardData;

        var sprite = Resources.Load<Sprite>( cardData.imagePath );
        newCard.GetComponent<Image>().sprite = Instantiate( sprite );

        //if( randomRotation )
         //   newCard.rotation = Utility.GetEnumValues<Side>().RandomItem();

        return newCard;
    }

    public void DrawCardToOpenHand()
    {
        var newCard = DrawCard( true );
        slots.Add( newCard );
        newCard.transform.SetParent( slotsPanelUI.transform );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TileSelectedEvent tileSelectedEvent )
        {
            foreach( var( idx, card ) in slots.Enumerate() )
            {
                if( tileSelectedEvent.card == card )
                {
                    selectedCardFromSlot = idx;
                    break;
                }
            }
        }
        else if( e is TilePlacedEvent tilePlaced )
        {
            if( !tilePlaced.wasPlacedOnBoard )
            {
                // + 1 to account for the placeholder deck card
                tilePlaced.card.transform.SetSiblingIndex( selectedCardFromSlot.Value + 1 );
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
