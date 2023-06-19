using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandler : MonoBehaviour
{
    [SerializeField] GameConstants constants;
    [SerializeField] HorizontalLayoutGroup slotsPanelUI;
    [SerializeField] CardComponent tilePrefab;
    private List<CardComponent> slots;
    private List<CardData> allCards;

    void Start()
    {
        slots = new List<CardComponent>();
        allCards = new List<CardData>();
        foreach( var card in DataHandler.GetAllCards() )
            allCards.Add( Instantiate( card ) );
        allCards.RandomShuffle();

        for( int i = 0; i < constants.deckNumStartingCards; ++i )
        {
            var newCard = DrawCard( true );
            slots.Add( newCard );
            newCard.transform.SetParent( slotsPanelUI.transform );
        }
    }

    public bool DeckEmpty()
    {
        return allCards.IsEmpty();
    }

    public CardComponent DrawCard( bool randomRotation )
    {
        var cardData = allCards.PopBack();
        var newCard = Instantiate( tilePrefab );
        newCard.data = cardData;

        var sprite = Resources.Load<Sprite>( cardData.imagePath );
        newCard.GetComponent<Image>().sprite = Instantiate( sprite );

        if( randomRotation )
            newCard.rotation = Utility.GetEnumValues<Side>().RandomItem();

        return newCard;
    }
}
