using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class BoardHandler : NetworkBehaviour, IEventReceiver
{
    [SerializeField] RectTransform grid;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] Image highlightTilePrefab;
    private Dictionary<Vector2Int, TileComponent> board;
    private Dictionary<Vector2Int, Image> highlights;
    private int boardSize;
    [SerializeField] string currentPlayerturn;
    private List<string> players;

    // Challenge data
    public class ChallengeData
    {
        public TileComponent tile;
        public Vector2Int gridPos;
        public int scoreTotal;
        public string challenger;
        public TileComponent tileCopy;
    };
    private Utility.FunctionTimer challengeTimer;
    private ChallengeData challengePhaseData;

    static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int( 0, 1 ),
        new Vector2Int( 1, 0 ),
        new Vector2Int( 0, -1 ),
        new Vector2Int( -1, 0 ),
    };

    protected void Start()
    {
        EventSystem.Instance.AddSubscriber( this );

        boardSize = 8;
    }

    private void ResetGame( List<string> playerNames )
    {
        if( board != null )
            foreach( var tile in board.Values )
                tile.DestroyObject();

        board = new Dictionary<Vector2Int, TileComponent>();
        PlaceTile( deck.DrawTile( GameController.Instance.gameRandom ), new Vector2Int( 0, 1 ) );
        PlaceTile( deck.DrawTile( GameController.Instance.gameRandom ), new Vector2Int( 0, 0 ) );
        PlaceTile( deck.DrawTile( GameController.Instance.gameRandom ), new Vector2Int( 1, 0 ) );
        PlaceTile( deck.DrawTile( GameController.Instance.gameRandom ), new Vector2Int( 1, 1 ) );

        players = playerNames;
        currentPlayerturn = players[0]; // Assume 0 is the host/first player, maybe randomise?
        EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
    }

    public bool TryPlaceTile( TileComponent tile, Vector2Int gridPos )
    {
        bool validPlacement = IsAvailableSpot( gridPos );

        EventSystem.Instance.TriggerEvent( new TilePlacedEvent()
        {
            tile = tile,
            successfullyPlaced = validPlacement,
            waitingForChallenge = validPlacement && challengePhaseData == null,
        } );

        // Valid placement, activate challenge
        if( validPlacement && challengePhaseData == null )
        {
            StartChallengeTimer( tile, gridPos );
            PlaceTile( tile, gridPos );
            tile.SetGhosted( true );
            tile.SetChallengeScoreText( challengePhaseData.scoreTotal );
            return true;
        }

        // Valid placement from challenge, compare to the original move
        if( validPlacement && challengePhaseData != null )
        {
            List<ScoreInfo> scoreResults = EvaluateScore( gridPos, challengePhaseData.tile );
            if( scoreResults.Sum( x => x.score ) <= challengePhaseData.scoreTotal )
            {
                ChallengeFailed();
                return true;
            }
        }

        if( validPlacement )
        {
            PlaceTile( tile, gridPos );
            List<ScoreInfo> scoreResults = EvaluateScore( gridPos, null );

            EventSystem.Instance.TriggerEvent( new PlayerScoreEvent()
            {
                player = currentPlayerturn,
                scoreModifiers = scoreResults,
                placedTile = challengePhaseData != null ? challengePhaseData.tile : tile,
                fromChallenge = challengePhaseData != null,
            } );
        }

        if( challengePhaseData != null )
            ChallengeSuccess( challengePhaseData.gridPos, challengePhaseData.tile );

        return validPlacement;
    }

    private void StartChallengeTimer( TileComponent tile, Vector2Int gridPos )
    {
        List<ScoreInfo> scoreResults = EvaluateScore( gridPos, tile );

        challengePhaseData = new ChallengeData()
        {
            gridPos = gridPos,
            scoreTotal = scoreResults.Sum( x => x.score ),
            tile = tile,
        };

        float challengeTime = ( GameController.Instance.isOfflineGame && GameController.Instance.isLocalPlayerTurn ) ? 
            GameConstants.Instance.challengeStartTimerSecLocal : GameConstants.Instance.challengeStartTimerSec;
        challengeTimer = Utility.FunctionTimer.CreateTimer( challengeTime, () =>
        {
            ChallengeFailed();
        } );
    }

    public void RequestChallenge( string player )
    {
        if( NetworkManager.Singleton.IsClient )
            RequestChallengeServerRpc();
        else
            Challenge( player ?? GameController.Instance.localPlayerName );
    }

    [ServerRpc( RequireOwnership = false )]
    void RequestChallengeServerRpc( ServerRpcParams rpcParams = default )
    {
        if( challengePhaseData.challenger != null )
            return;

        var player = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId];
        var name = player.PlayerObject.GetComponent<PlayerController>().playerName;
        challengePhaseData.challenger = name;
        ChallengeClientRpc( name );
    }

    [ClientRpc]
    void ChallengeClientRpc( string player )
    {
        Challenge( player );
    }

    private void Challenge( string player )
    {
        Debug.Assert( challengePhaseData != null );
        challengePhaseData.challenger = player;
        challengeTimer.Stop();

        challengeTimer = Utility.FunctionTimer.CreateTimer( GameConstants.Instance.challengeActionTimerSec, () =>
        {
            ChallengeFailed();
        } );

        EventSystem.Instance.TriggerEvent( new ChallengeStartedEvent()
        {
            player = player,
            challengeData = challengePhaseData,
        } );

        if( GameController.Instance.localPlayerName == player )
        {
            board.Remove( challengePhaseData.gridPos );
            challengePhaseData.tileCopy = Instantiate( challengePhaseData.tile, challengePhaseData.tile.transform.parent, true );
            challengePhaseData.tileCopy.data = null;
            challengePhaseData.tile.SetInteractable( true );
            challengePhaseData.tile.SetGhosted( false );
            challengePhaseData.tile.ClearChallengeScoreText();
            challengePhaseData.tile.GetComponent<TileComponent>().OnDragStart();
            challengePhaseData.tile.GetComponent<Draggable>().ResetOffset();
        }
    }

    private void ChallengeCleanup()
    {
        Debug.Assert( challengePhaseData != null );
        challengePhaseData.tile.SetGhosted( false );
        challengePhaseData.tile.ClearChallengeScoreText();
        if( challengePhaseData.tileCopy != null )
            challengePhaseData.tileCopy.DestroyObject();
        challengePhaseData = null;
        challengeTimer.Stop();
    }

    private void ChallengeFailed()
    {
        var data = challengePhaseData;
        if( challengePhaseData.challenger != null )
            PlaceTile( data.tile, data.gridPos );
        ChallengeCleanup();

        EventSystem.Instance.TriggerEvent( new TilePlacedEvent()
        {
            tile = data.tile,
            successfullyPlaced = true,
        } );

        List<ScoreInfo> scoreResults = EvaluateScore( data.gridPos, null );

        EventSystem.Instance.TriggerEvent( new PlayerScoreEvent()
        {
            player = currentPlayerturn,
            scoreModifiers = scoreResults,
            placedTile = data.tile,
            fromChallenge = false,
        } );

        NextTurn();
    }

    private void ChallengeSuccess( Vector2Int pos, TileComponent tile )
    {
        ChallengeCleanup();
        NextTurn();
    }

    private void NextTurn()
    {
        Utility.FunctionTimer.CreateTimer( GameConstants.Instance.turnChangeDelaySec, () =>
        {
            currentPlayerturn = players[( players.FindIndex( x => x == currentPlayerturn ) + 1 ) % players.Count];
            EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
        } );
    }

    private void PlaceTile( TileComponent tile, Vector2Int pos )
    {
        SetPositionOnGrid( tile, pos );
        tile.SetInteractable( false );
        board.Add( pos, tile );
    }

    private void SetPositionOnGrid( TileComponent tile, Vector2Int pos )
    {
        tile.SetData( TileSource.Board, pos );
        tile.transform.SetParent( grid, false );
        tile.transform.localPosition = GetPositionOnGrid( pos );
    }

    public Vector2 GetPositionOnGrid( Vector2Int pos )
    {
        return new Vector2(
            pos.x * ( cellSize.x + padding.x ),
            pos.y * ( cellSize.y + padding.y ) ) - cellSize / 2.0f;
    }

    public Vector2Int GetGridPosition( Vector2 pos )
    {
        pos += cellSize / 2.0f;
        return new Vector2Int(
            Mathf.RoundToInt( pos.x / ( cellSize.x + padding.x ) ),
            Mathf.RoundToInt( pos.y / ( cellSize.y + padding.y ) ) );
    }

    public bool ResetTile( Vector2Int pos )
    {
        if( board.TryGetValue( pos, out var tile ) )
        {
            tile.DestroyObject();
            return true;
        }
        return false;
    }

    private bool IsAvailableSpot( Vector2Int pos )
    {
        if( pos.x > boardSize / 2 ||
            pos.y > boardSize / 2 ||
            pos.x <= -boardSize / 2 ||
            pos.y <= -boardSize / 2 )
            return false;

        if( board.ContainsKey( pos ) )
            return false;

        if( challengePhaseData != null && pos == challengePhaseData.gridPos )
            return false;

        return directions.Any( x => board.ContainsKey( pos + x ) );
    }

    private void HighlightAvailableSpot( Vector2Int pos )
    {
        if( highlights.ContainsKey( pos ) )
            return;

        var highlight = Instantiate( highlightTilePrefab );
        highlight.transform.SetParent( grid, false );
        highlight.transform.localPosition = GetPositionOnGrid( pos );
        highlights.Add( pos, highlight );
    }

    public void HighlightAvailableSpots()
    {
        if( highlights != null )
            return;

        highlights = new Dictionary<Vector2Int, Image>();

        foreach( var pos in GetAvailableSpots() )
            HighlightAvailableSpot( pos );
    }

    public List<Vector2Int> GetAvailableSpots()
    {
        var spots = new List<Vector2Int>();
        var tried = new HashSet<Vector2Int>();

        foreach( var (pos, _) in board )
        {
            foreach( var direction in directions )
            {
                var newPos = pos + direction;
                if( !tried.Contains( newPos ) )
                {
                    if( IsAvailableSpot( newPos ) )
                        spots.Add( newPos );
                    tried.Add( newPos );
                }
            }
        }

        return spots;
    }

    public void RemoveHighlights()
    {
        if( highlights == null )
            return;

        foreach( var (_, highlight) in highlights )
            highlight.DestroyObject();
        highlights = null;
    }

    void IEventReceiver.OnEventReceived( IBaseEvent e )
    {
        if( e is TileSelectedEvent tileSelected )
        {
            if( tileSelected.showHighlights )
                HighlightAvailableSpots();
        }
        else if( e is TileDroppedEvent tilePlacedEvent )
        {
            TileDropped( tilePlacedEvent );
        }
        else if( e is StartGameEvent startGame )
        {
            ResetGame( startGame.playerData.Select( x => x.name ).ToList() );
        }
        else if( e is ExitGameEvent exitGame )
        {

        }
    }

    private TileComponent FindTileFromNetworkData( TileNetworkData data )
    {
        Debug.Assert( data.source != TileSource.Deck );
        if( data.source == TileSource.Hand )
            return deck.FindTileInOpenHand( data );
        else
            return board[data.location];
    }

    private void TileDropped( TileDroppedEvent tilePlacedEvent )
    {
        RemoveHighlights();
        var gridPos = GetGridPosition( grid.worldToLocalMatrix.MultiplyPoint( tilePlacedEvent.tile.transform.position ) );

        // Host can just tell connected clients (including itself, directly)
        //if( NetworkManager.Singleton.IsHost )
        //    TileDroppedClientRpc( tilePlacedEvent.tile.networkData, gridPos );
        // Request place tile from server
        if( NetworkManager.Singleton.IsClient )
            TileDroppedRequestServerRpc( tilePlacedEvent.tile.networkData, gridPos );
        else // Local code path
            TryPlaceTile( tilePlacedEvent.tile, gridPos );
    }

    private bool TryPlaceTileOtherPlayer( TileNetworkData tile, Vector2Int gridPos )
    {
        var tileCmp = FindTileFromNetworkData( tile );
        if( tileCmp == null )
            return false;
        EventSystem.Instance.TriggerEvent( new TileSelectedEvent() { tile = tileCmp, showHighlights = false } );
        return TryPlaceTile( tileCmp, gridPos );
    }

    // Used by AI to send move to all clients
    public void TryPlaceTileServer( TileComponent tile, Vector2Int gridPos )
    {
        Debug.Assert( GameController.Instance.isOfflineGame || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost );
        if( NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost )
            TileDroppedClientRpc( tile.networkData, gridPos );
        else
            TryPlaceTileOtherPlayer( tile.networkData, gridPos );
    }

    [ServerRpc( RequireOwnership = false )]
    private void TileDroppedRequestServerRpc( TileNetworkData tile, Vector2Int gridPos, ServerRpcParams rpcParams = default )
    {
        if( TryPlaceTileOtherPlayer( tile, gridPos ) )
        {
            //var clientsToSendTo = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            //clientsToSendTo.Remove( rpcParams.Receive.SenderClientId );
            TileDroppedClientRpc( tile, gridPos );//, new ClientRpcParams() 
            //{ 
            //    Send = new ClientRpcSendParams() { TargetClientIds = clientsToSendTo } 
            //} );
        }
    }

    [ClientRpc]
    public void TileDroppedClientRpc( TileNetworkData tile, Vector2Int gridPos, ClientRpcParams rpcParams = default )
    {
        TryPlaceTileOtherPlayer( tile, gridPos );
    }

    public List<ScoreInfo> EvaluateScore( Vector2Int pos, TileComponent testCard )
    {
        if( testCard != null )
            board.Add( pos, testCard );

        List<ScoreInfo> scoreResults = new List<ScoreInfo>();
        scoreResults.AddRange( ScorePatternRule( pos ) );
        scoreResults.AddRange( ScoreOneSideRule( pos ) );
        scoreResults.AddRange( ScoreDiffRule( pos ) );
        scoreResults.AddRange( ScoreSameRule( pos ) );

        if( testCard != null )
            board.Remove( pos );

        return scoreResults;
    }

    private TileSide GetCurrentSide( Vector2Int pos, Side direction )
    {
        if( !board.TryGetValue( pos, out var value ) )
            return null;

        return value.data.sides[Utility.Mod( ( int )direction - ( int )value.rotation, Utility.GetNumEnumValues<Side>() )];
    }

    private TileSide GetAdjacentSide( Vector2Int pos, Side direction )
    {
        return GetCurrentSide( pos + directions[( int )direction], direction.Opposite() );
    }

    private void GetOpposingCardSides( Vector2Int pos, Side direction, out TileSide side, out TileSide other )
    {
        side = GetCurrentSide( pos, direction );
        other = GetAdjacentSide( pos, direction );
    }

    private List<ScoreInfo> ScoreOneSideRule( Vector2Int pos )
    {
        TileSide singleSide = null;
        foreach( var side in Utility.GetEnumValues<Side>() )
        {
            if( ValidSide( GetAdjacentSide( pos, side ) != null ? GetCurrentSide( pos, side ) : null, out var sideTile ) )
            {
                // If singleSide already set, then we know we must be touching multiple sides (so no bonus score)
                if( singleSide != null )
                    return new List<ScoreInfo>();
                singleSide = sideTile;
            }
        }

        return new List<ScoreInfo>()
        {
            new ScoreInfo()
            {
                score = GameConstants.Instance.oneSideExtraScore,
                sides = new List<TileSide>() { singleSide },
                source = ScoreSource.SingleSideBonus,
            }
        };
    }

    private ScoreInfo ScoreDiffRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( ValidSide( other, out var _ ) && side.colour != other.colour && side.value != other.value )
        {
            return new ScoreInfo()
            {
                score = Mathf.Abs( side.value - other.value ),
                sides = new List<TileSide>() { side, other },
                source = ScoreSource.SideDifference
            };
        }
        return null;
    }

    private List<ScoreInfo> ScoreDiffRule( Vector2Int pos )
    {
        List<ScoreInfo> results = new List<ScoreInfo>();
        foreach( var side in Utility.GetEnumValues<Side>() )
        {
            var diffScoreResult = ScoreDiffRuleSide( pos, side );
            if( diffScoreResult != null )
                results.Add( diffScoreResult );
        }
        return results;
    }

    private ScoreInfo ScoreSameRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( ValidSide( other, out var _ ) && side.colour == other.colour && side.value == other.value )
        {
            return new ScoreInfo()
            {
                score = side.value,
                sides = new List<TileSide>() { side, other },
                source = ScoreSource.MatchingSide
            };
        }
        return null;
    }

    private List<ScoreInfo> ScoreSameRule( Vector2Int pos )
    {
        List<ScoreInfo> results = new List<ScoreInfo>();
        foreach( var side in Utility.GetEnumValues<Side>() )
        {
            var sameSideScore = ScoreSameRuleSide( pos, side );
            if( sameSideScore != null )
                results.Add( sameSideScore );
        }
        return results;
    }

    private ScoreInfo ScorePatternRuleSide( Vector2Int pos, Side side, Vector2Int direction )
    {
        List<TileSide> sides = new List<TileSide>();
        int? value = null;
        int? colour = null;
        for( int i = 0; i < GameConstants.Instance.patternLengthMin; ++i )
        {
            var next = GetCurrentSide( pos + direction * i, side );
            if( !ValidSide( next, out var _ ) )
                return null;

            if( i == 0 )
            {
                value = next.value;
                colour = next.colour;
            }
            else
            {
                if( value != null && next.value != value )
                    value = null;
                if( colour != null && next.colour != colour )
                    colour = null;
                if( value == null && colour == null )
                    return null;
            }
            sides.Add( next );
        }

        return new ScoreInfo()
        {
            score = GameConstants.Instance.patternExtraScore,
            sides = sides,
            source = ScoreSource.Pattern
        };
    }

    private List<ScoreInfo> ScorePatternRule( Vector2Int pos )
    {
        List<ScoreInfo> results = new List<ScoreInfo>();
        foreach( var side in Utility.GetEnumValues<Side>() )
        {
            var forwardPattern = ScorePatternRuleSide( pos, side, directions[side.Value()] );
            var backwardPattern = ScorePatternRuleSide( pos, side.Opposite(), directions[side.Value()] );
            if( forwardPattern != null )
                results.Add( forwardPattern );
            if( backwardPattern != null )
                results.Add( backwardPattern );

            if( side == Side.Down || side == Side.Right )
            {
                var forwardCentrePattern = ScorePatternRuleSide( pos - directions[side.Value()], side, directions[side.Value()] );
                var backwardCentrePattern = ScorePatternRuleSide( pos - directions[side.Value()], side.Opposite(), directions[side.Value()] );
                if( forwardCentrePattern != null )
                    results.Add( forwardCentrePattern );
                if( backwardCentrePattern != null )
                    results.Add( backwardCentrePattern );
            }
        }

        return results;
    }

    private bool ValidSide( TileSide tile, out TileSide outTile )
    {
        outTile = tile;
        return tile != null;
    }
}
