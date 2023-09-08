
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

class TileSelectedEvent : IBaseEvent
{
    public TileComponent tile;
    public bool showHighlights;
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

class TurnStartEvent : IBaseEvent
{
    public string player = null;
}

class LobbyUpdatedEvent : IBaseEvent
{
    public Lobby lobby;
    public List<NetworkHandler.PlayerData> playerData;
}

class PlayerScoreEvent : IBaseEvent
{
    public TileComponent placedTile;
    public string player;
    public List<ScoreInfo> scoreModifiers;
}

class PreStartGameEvent : IBaseEvent { }

class StartGameEvent : IBaseEvent
{
    public List<NetworkHandler.PlayerData> playerData;
}