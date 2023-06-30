
class TileSelectedEvent : IBaseEvent
{
    public TileComponent tile;
}

class TileDroppedEvent : IBaseEvent
{
    public TileComponent tile;
}

class TilePlacedEvent : IBaseEvent
{
    public TileComponent tile;
    public bool successfullyPlaced;
}

class PlayerScoreEvent : IBaseEvent
{
    public int playerIdx;
    public int scoreModifier;
}