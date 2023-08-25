
using Unity.Services.Lobbies.Models;

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

class LobbyUpdatedEvent : IBaseEvent
{
    public Lobby lobby;
}

class PlayerScoreEvent : IBaseEvent
{
    public int playerIdx;
    public int scoreModifier;
}