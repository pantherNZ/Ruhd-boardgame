
class TileSelectedEvent : IBaseEvent
{
    public CardComponent card;
}

class TileDroppedEvent : IBaseEvent
{
    public CardComponent card;
}

class TilePlacedEvent : IBaseEvent
{
    public CardComponent card;
    public bool wasPlacedOnBoard;
}

class PlayerScoreEvent : IBaseEvent
{
    public int playerIdx;
    public int scoreModifier;
}