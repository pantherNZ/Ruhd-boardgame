﻿
using System.Collections.Generic;
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

class TurnStartEvent : IBaseEvent
{
    public string player = null;
}

class LobbyUpdatedEvent : IBaseEvent
{
    public Lobby lobby;
    public List<string> playerNames;
}

class PlayerScoreEvent : IBaseEvent
{
    public int playerIdx;
    public int scoreModifier;
}

class StartGameEvent : IBaseEvent
{
    public List<string> playerNames;
}