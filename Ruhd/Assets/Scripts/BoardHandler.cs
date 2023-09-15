using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

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
    private Vector2Int? lastPlaced;

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

        boardSize = 8;// Mathf.Min(
            //Mathf.FloorToInt( grid.rect.width / cellSize.x ),
            //Mathf.FloorToInt( grid.rect.height / cellSize.y ) );
    }

    private void ResetGame( List<string> playerNames )
    {
        board = new Dictionary<Vector2Int, TileComponent>();
        PlaceTile( new Vector2Int( 0, 1 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 0, 0 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 1, 0 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 1, 1 ), deck.DrawTile( true ) );

        players = playerNames;
        currentPlayerturn = players[0]; // Assume 0 is the host/first player, maybe randomise?
        EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
    }

    private bool TryPlaceTile( TileComponent tile, Vector2Int gridPos )
    {
        bool validPlacement = IsAvailableSpot( gridPos );

        EventSystem.Instance.TriggerEvent( new TilePlacedEvent()
        {
            tile = tile,
            successfullyPlaced = validPlacement,
        } );

        // Valid placement
        if( validPlacement )
        {
            PlaceTile( gridPos, tile );
            List<ScoreInfo> scoreResults = EvaluateScore( gridPos, null );

            if( scoreResults != null && scoreResults.Count > 0 )
            {
                EventSystem.Instance.TriggerEvent( new PlayerScoreEvent()
                {
                    player = currentPlayerturn,
                    scoreModifiers = scoreResults,
                    placedTile = tile,
                } );
            }

            NextTurnStage();

            if( board.Count >= boardSize * boardSize )
            {
                EventSystem.Instance.TriggerEvent( new GameOverEvent() );
            }
        }

        lastPlaced = gridPos;
        return validPlacement;
    }

    private void NextTurnStage()
    {
        currentPlayerturn = players[( players.FindIndex( x => x == currentPlayerturn ) + 1 ) % players.Count];
        EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
    }

    private void PlaceTile( Vector2Int pos, TileComponent tile )
    {
        SetPositionOnGrid( pos, tile );
        tile.SetInteractable( false );
        board.Add( pos, tile );
    }

    private void SetPositionOnGrid( Vector2Int pos, TileComponent tile )
    {
        tile.SetData( TileSource.Board, pos );
        tile.transform.SetParent( grid, false );
        tile.transform.localPosition = GetPositionOnGrid( pos );
    }

    private void ReenablePatterns( Vector2Int pos )
    {
        for( int i = 0; i < boardSize; ++i )
        {
            var curSide = GetCurrentSide( new Vector2Int( pos.x, i ), Side.Down );
            if( curSide != null )
                curSide.patternUsed = false;
            curSide = GetCurrentSide( new Vector2Int( pos.x, i ), Side.Up );
            if( curSide != null )
                curSide.patternUsed = false;
            curSide = GetCurrentSide( new Vector2Int( i, pos.y ), Side.Left );
            if( curSide != null )
                curSide.patternUsed = false;
            curSide = GetCurrentSide( new Vector2Int( i, pos.y ), Side.Right );
            if( curSide != null )
                curSide.patternUsed = false;
        }
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
        return directions.Any( x => board.ContainsKey( pos + x ) );
    }

    private void HighlightAvailableSpot( Vector2Int pos )
    {
        if( highlights.ContainsKey( pos ) )
            return;

        if( !IsAvailableSpot( pos ) )
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

        foreach( var (pos, _) in board )
            foreach( var direction in directions )
                HighlightAvailableSpot( pos + direction );
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
        else if( e is PreStartGameEvent )
        {
            Random.InitState( GameConstants.Instance.rngSeedRuntime );
        }
        else if( e is StartGameEvent startGame )
        {
            ResetGame( startGame.playerData.Select( x => x.name ).ToList() );
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
    private void TileDroppedClientRpc( TileNetworkData tile, Vector2Int gridPos, ClientRpcParams rpcParams = default )
    {
        TryPlaceTileOtherPlayer( tile, gridPos );
    }

    public List<ScoreInfo> EvaluateScore( Vector2Int pos, TileComponent testCard )
    {
        if( testCard != null )
            board.Add( pos, testCard );

        List<ScoreInfo> scoreResults = new List<ScoreInfo>();
        scoreResults.AddRange( ScorePatternRule( pos, testCard == null ) );
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

    private ScoreInfo ScorePatternRuleSide( Vector2Int pos, Side side, Vector2Int direction, bool consumePatterns )
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

        if( consumePatterns )
        {
            var min = new Vector2Int( 
                direction.x == 0 ? pos.x : boardSize / 2 * direction.x, 
                direction.y == 0 ? pos.y : boardSize / 2 * direction.y );

            for( int i = 0; i < boardSize; ++i )
            {
                if( ValidSide( GetCurrentSide( min + direction * i, side ), out var tile ) )
                    tile.patternUsed = true;
            }
        }

        return new ScoreInfo()
        {
            score = GameConstants.Instance.patternExtraScore,
            sides = sides,
            source = ScoreSource.Pattern
        };
    }

    private List<ScoreInfo> ScorePatternRule( Vector2Int pos, bool consumePatterns )
    {
        List<ScoreInfo> results = new List<ScoreInfo>();
        foreach( var side in Utility.GetEnumValues<Side>() )
        {
            var forwardPattern = ScorePatternRuleSide( pos, side, directions[side.Value()], consumePatterns );
            var backwardPattern = ScorePatternRuleSide( pos, side.Opposite(), directions[side.Value()], consumePatterns );
            if( forwardPattern != null )
                results.Add( forwardPattern );
            if( backwardPattern != null )
                results.Add( backwardPattern );

            if( side == Side.Down || side == Side.Right )
            {
                var forwardCentrePattern = ScorePatternRuleSide( pos - directions[side.Value()], side, directions[side.Value()], consumePatterns );
                var backwardCentrePattern = ScorePatternRuleSide( pos - directions[side.Value()], side.Opposite(), directions[side.Value()], consumePatterns );
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
