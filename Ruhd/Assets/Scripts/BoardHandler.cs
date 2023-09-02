using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class BoardHandler : NetworkBehaviour, IEventReceiver
{
    [SerializeField] GameConstants constants;
    [SerializeField] RectTransform grid;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] Image highlightTilePrefab;
    private Dictionary<Vector2Int, TileComponent> board;
    private Dictionary<Vector2Int, TileComponent> flipped;
    private Dictionary<Vector2Int, Image> highlights;
    private int boardSize;
    [SerializeField] bool placementAction = true; // False means flip tile action
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

        boardSize = Mathf.Min(
            Mathf.FloorToInt( grid.rect.width / cellSize.x ),
            Mathf.FloorToInt( grid.rect.height / cellSize.y ) );
        Reset();
    }

    private void Reset()
    {
        board = new Dictionary<Vector2Int, TileComponent>();
        flipped = new Dictionary<Vector2Int, TileComponent>();
        PlaceTile( new Vector2Int( 0, 1 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 0, 0 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 1, 0 ), deck.DrawTile( true ) );
        PlaceTile( new Vector2Int( 1, 1 ), deck.DrawTile( true ) );
    }

    private bool TryPlaceTile( TileComponent tile, Vector2Int gridPos )
    {
        bool validPlacement = IsAvailableSpot( gridPos );

        EventSystem.Instance.TriggerEvent( new TilePlacedEvent()
        {
            tile = tile,
            successfullyPlaced = validPlacement,
        } );

        // Flipped tile failed to place, return it to where it was on the board before
        if( tile.flipped )
        {
            var found = flipped.First( v => tile == v.Value );

            if( validPlacement )
            {
                tile.ShowFront( true );
                flipped.Remove( found.Key );
            }
            else
            {
                tile.ShowBack();
                SetPositionOnGrid( found.Key, tile );
            }
        }
        
        // Valid placement
        if( validPlacement )
        {
            PlaceTile( gridPos, tile );
            int gainedScore = EvaluateScore( gridPos, null );

            if( gainedScore > 0 )
            {
                EventSystem.Instance.TriggerEvent( new PlayerScoreEvent()
                {
                    player = currentPlayerturn,
                    scoreModifier = gainedScore,
                } );
            }

            NextTurnStage();
        }

        lastPlaced = gridPos;
        return validPlacement;
    }

    private void NextTurnStage()
    {
        if( !placementAction )
        {
            currentPlayerturn = players[( players.FindIndex( x => x == currentPlayerturn ) + 1 ) % players.Count];
            EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
        }

        placementAction = !placementAction;

        foreach( var tile in flipped.Values )
            tile.SetInteractable( placementAction );
    }

    private void PlaceTile( Vector2Int pos, TileComponent tile )
    {
        SetPositionOnGrid( pos, tile );
        tile.SetInteractable( false );

        if( placementAction )
        {
            board.Add( pos, tile );
        }
        else
        {
            flipped.Add( pos, tile );
            tile.ShowBack();
            ReenablePatterns( pos );
        }
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

        if( flipped.ContainsKey( pos ) )
            return false;

        if( placementAction )
        {
            if( board.ContainsKey( pos ) )
                return false;
            return directions.Any( x => board.ContainsKey( pos + x ) );
        }
        else if( !lastPlaced.HasValue || pos != lastPlaced.Value )
        {
            return board.ContainsKey( pos );
        }

        return false;
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
        {
            if( placementAction )
            {
                foreach( var direction in directions )
                    HighlightAvailableSpot( pos + direction );
            }
            else
            {
                HighlightAvailableSpot( pos );
            }
        }
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

            if( tileSelected.tile.flipped )
                tileSelected.tile.ShowFront( false );
        }
        else if( e is TileDroppedEvent tilePlacedEvent )
        {
            TileDropped( tilePlacedEvent );
        }
        else if( e is StartGameEvent startGame )
        {
            players = startGame.playerNames;
            currentPlayerturn = players[0]; // Assume 0 is the host/first player, maybe randomise?
            EventSystem.Instance.TriggerEvent( new TurnStartEvent() { player = currentPlayerturn } );
        }
    }

    private TileComponent FindTileFromNetworkData( TileNetworkData data )
    {
        Debug.Assert( data.source != TileSource.Deck );
        if( data.source == TileSource.Hand )
            return deck.FindTileInOpenHand( data );
        else if( data.flipped )
            return flipped[data.location];
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

    public int EvaluateScore( Vector2Int pos, TileComponent newCard )
    {
        if( !placementAction )
            return 0;

        if( newCard != null )
            board.Add( pos, newCard );

        int patternScore = ScorePatternRule( pos, newCard != null );
        int oneSideScore = ScoreOneSideRule( pos );
        int diffScore = ScoreDiffRule( pos );
        int sameScore = ScoreSameRule( pos );
        int finalScore = patternScore + oneSideScore + diffScore + sameScore;

        Debug.Log( "Score: " + finalScore + 
            "\n    - Diff colour: " + diffScore +
            "\n    - Extra from one side: " + ScoreOneSideRule( pos ) +
            "\n    - Same colour: " + sameScore +
            "\n    - Pattern: " + patternScore );

        if( newCard != null )
            board.Remove( pos );

        return finalScore;
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

    private int ScoreOneSideRule( Vector2Int pos )
    {
        int sides = Utility.GetEnumValues<Side>().Sum( side => GetAdjacentSide( pos, side ) != null ? 1 : 0 );
        return sides == 1 ? constants.oneSideExtraScore : 0;
    }

    private int ScoreDiffRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( other != null && side.colour != other.colour )
            return Mathf.Abs( side.value - other.value );
        return 0;
    }

    private int ScoreDiffRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreDiffRuleSide( pos, side ) );
    }

    private int ScoreSameRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( other != null && side.colour == other.colour && side.value == other.value )
            return side.value;
        return 0;
    }

    private int ScoreSameRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreSameRuleSide( pos, side ) );
    }

    private int ScorePatternRuleSide( Vector2Int pos, Side side, Vector2Int direction, bool consumePatterns )
    {
        var current = GetCurrentSide( pos, side );
        if( current.patternUsed )
            return 0;

        bool valueMatch = true;
        bool colourMatch = true;
        for( int i = 1; i < constants.patternLengthMin; ++i )
        {
            var next = GetCurrentSide( pos + direction * i, side );
            if( next == null )
                return 0;
            if( next.patternUsed )
                return 0;
            valueMatch &= current.value == next.value;
            colourMatch &= current.colour == next.colour;
            if( !valueMatch && !colourMatch )
                return 0;
        }

        if( consumePatterns )
        {
            var min = new Vector2Int( 
                direction.x == 0 ? pos.x : boardSize / 2 * direction.x, 
                direction.y == 0 ? pos.y : boardSize / 2 * direction.y );

            for( int i = 0; i < boardSize; ++i )
            {
                var curSide = GetCurrentSide( min + direction * i, side );
                if( curSide != null )
                    curSide.patternUsed = true;
            }
        }

        return constants.patternExtraScore;
    }

    private int ScorePatternRule( Vector2Int pos, bool consumePatterns )
    {
        return Utility.GetEnumValues<Side>().Sum( side =>
            Mathf.Max(
                ScorePatternRuleSide( pos, side, directions[side.Value()], consumePatterns ),
                ScorePatternRuleSide( pos, side.Opposite(), directions[side.Value()], consumePatterns ) )
            );
    }
}
