
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
    public bool waitingForChallenge;
}

class TurnStartEvent : IBaseEvent
{
    public string player;
}

class PlayerDisconnectedEvent : IBaseEvent
{
    public string player;
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
    public bool fromChallenge;
}

class RequestStartGameEvent : IBaseEvent
{
    public List<NetworkHandler.PlayerData> playerData;
}

class PreStartGameEvent : IBaseEvent { }

class StartGameEvent : IBaseEvent
{
    public List<NetworkHandler.PlayerData> playerData;
    public bool vsComputer;
}

class ExitGameEvent : IBaseEvent 
{
    public bool fromGameOver;
}

class GameOverEvent : IBaseEvent { }

class ChallengeStartedEvent : IBaseEvent
{
    public string player;
    public BoardHandler.ChallengeData challengeData;
}

class RequestTogglePauseGameEvent : IBaseEvent { }