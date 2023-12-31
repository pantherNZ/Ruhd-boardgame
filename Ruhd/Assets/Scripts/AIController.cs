﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class AIController : EventReceiverInstance
{
    [HideInInspector] public BoardHandler board;
    [HideInInspector] public DeckHandler deck;
    [HideInInspector] public string playerName;
    [HideInInspector] public float difficulty; // 0 is worst, 1 is optimal
    [HideInInspector] public float deviation;

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TurnStartEvent turnStart )
        {
            // Only process AI on our turn and on the server (or local game)
            if( turnStart.player == playerName )
            {
                if( GameController.Instance.isOfflineGame || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost )
                    Utility.FunctionTimer.CreateTimer( Random.Range( GameConstants.Instance.AIThinkTimeMinSec, GameConstants.Instance.AIThinkTimeMaxSec ), ProcessAI );
            }
        }
    }

    struct Move
    {
        public int score;
        public Vector2Int pos;
        public Side rot;
        public TileComponent tile;
    }

    private void ProcessAI()
    {
        var spots = board.GetAvailableSpots();
        List<Move> moves = new List<Move>();

        foreach( var spot in spots )
        {
            foreach( var tile in deck.GetOpenHand() )
            {
                var originalRot = tile.rotation;
                foreach( var rot in SideUtil.GetValues() )
                {
                    tile.rotation = rot;
                    var score = board.EvaluateScore( spot, tile );
                    moves.Add( new Move()
                    {
                        pos = spot,
                        rot = rot,
                        tile = tile,
                        score = score.Sum( x => x.score )
                    } );
                }
                tile.rotation = originalRot;
            }
        }

        moves.Sort( ( a, b ) => a.score - b.score );
        var moveSelection = Mathf.Clamp( Utility.DefaultRng.Gaussian( difficulty, deviation ), 0.0f, 1.0f );
        var move = moves[Mathf.RoundToInt( moveSelection * ( moves.Count - 1 ) )];
        move.tile.rotation = move.rot;
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { tile = move.tile } );
        board.TryPlaceTileServer( move.tile, move.pos );
        Debug.Log( $"AI MOVE: Pos: {move.pos}, Rot: {move.rot}, Score:{move.score}" );
    }
}