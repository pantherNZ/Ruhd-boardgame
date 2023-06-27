using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardHandler : EventReceiverInstance
{
    [SerializeField] GameConstants constants;
    [SerializeField] RectTransform grid;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] Image highlightTilePrefab;
    Dictionary<Vector2Int, CardComponent> board;
    Dictionary<Vector2Int, Image> highlights;
    int boardSize;

    static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int( 0, 1 ),
        new Vector2Int( 1, 0 ),
        new Vector2Int( 0, -1 ),
        new Vector2Int( -1, 0 ),
    };

    protected override void Start()
    {
        base.Start();

        boardSize = Mathf.Min(
            Mathf.FloorToInt( grid.rect.width / cellSize.x ),
            Mathf.FloorToInt( grid.rect.height / cellSize.y ) );
        Reset();
    }

    private void Reset()
    {
        board = new Dictionary<Vector2Int, CardComponent>();
        PlaceTile( new Vector2Int( 0, 1 ), deck.DrawCard( true ) );
        PlaceTile( new Vector2Int( 0, 0 ), deck.DrawCard( true ) );
        PlaceTile( new Vector2Int( 1, 0 ), deck.DrawCard( true ) );
        PlaceTile( new Vector2Int( 1, 1 ), deck.DrawCard( true ) );
    }

    private bool TryPlaceTile( int playerIdx, CardComponent tile )
    {
        var gridPos = GetGridPosition( grid.worldToLocalMatrix.MultiplyPoint( tile.transform.position ) );
        bool validPlacement = IsAvailableSpot( gridPos );

        EventSystem.Instance.TriggerEvent( new TilePlacedEvent()
        {
            card = tile,
            wasPlacedOnBoard = validPlacement
        } );

        if( validPlacement )
        {
            PlaceTile( gridPos, tile );
            int gainedScore = EvaluateScore( gridPos, null );

            EventSystem.Instance.TriggerEvent( new PlayerScoreEvent()
            {
                playerIdx = playerIdx,
                scoreModifier = gainedScore,
            } );
        }

        return validPlacement;
    }

    private void PlaceTile( Vector2Int pos, CardComponent tile )
    {
        tile.transform.SetParent( grid );
        tile.transform.localPosition = GetPositionOnGrid( pos );
        tile.GetComponent<Draggable>().enabled = false;
        tile.GetComponent<EventDispatcherV2>().enabled = false;
        board.Add( pos, tile );
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
        highlight.transform.SetParent( grid );
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

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TileSelectedEvent tileSelectedEvent )
        {
            HighlightAvailableSpots();
        }
        else if( e is TileDroppedEvent tilePlacedEvent )
        {
            TryPlaceTile( 0, tilePlacedEvent.card );
            RemoveHighlights();
        }
    }

    public int EvaluateScore( Vector2Int pos, CardComponent newCard )
    {
        if( newCard != null )
            board.Add( pos, newCard );

        int score = ScoreOneSideRule( pos ) +
            ScoreDiffRule( pos ) +
            ScoreSameRule( pos ) +
            ScorePatternRule( pos );
        Debug.Log( "Score: " + score + 
            "\n    - Diff colour: " + ScoreDiffRule( pos ) +
            "\n    - Extra from one side: " + ScoreOneSideRule( pos ) +
            "\n    - Same colour: " + ScoreSameRule( pos ) +
            "\n    - Pattern: " + ScorePatternRule( pos ) );

        if( newCard != null )
            board.Remove( pos );

        return score;
    }

    private CardSide? GetCurrentSide( Vector2Int pos, Side direction )
    {
        if( !board.TryGetValue( pos, out var value ) )
            return null;

        return value.data.sides[Utility.Mod( ( int )direction - ( int )value.rotation, Utility.GetNumEnumValues<Side>() )];
    }

    private CardSide? GetAdjacentSide( Vector2Int pos, Side direction )
    {
        return GetCurrentSide( pos + directions[( int )direction], ( Side )Utility.Mod( ( int )direction + 2, 4 ) );
    }

    private void GetOpposingCardSides( Vector2Int pos, Side direction, out CardSide? side, out CardSide? other )
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
        if( other != null && side.Value.colour != other.Value.colour )
            return Mathf.Abs( side.Value.value - other.Value.value );
        return 0;
    }

    private int ScoreDiffRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreDiffRuleSide( pos, side ) );
    }

    private int ScoreSameRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( other != null && side.Value.colour == other.Value.colour && side.Value.value == other.Value.value )
            return side.Value.value;
        return 0;
    }

    private int ScoreSameRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreSameRuleSide( pos, side ) );
    }

    private int ScorePatternRuleSide( Vector2Int pos, Side side, Vector2Int direction )
    {
        var curLeft = GetCurrentSide( pos, side );
        var nextLeft = GetCurrentSide( pos + direction, side );
        var nextNextLeft = GetCurrentSide( pos + direction * 2, side );
        return curLeft.HasValue && nextLeft.HasValue && nextNextLeft.HasValue &&
            (
                ( curLeft.Value.colour == nextLeft.Value.colour && curLeft.Value.colour == nextNextLeft.Value.colour ) ||
                ( curLeft.Value.value == nextLeft.Value.value && curLeft.Value.value == nextNextLeft.Value.value )
            ) ? constants.patternExtraScore : 0;
    }

    private int ScorePatternRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side =>
            Mathf.Max(
                ScorePatternRuleSide( pos, side, directions[( int )Side.Up]),
                ScorePatternRuleSide( pos, side, directions[( int )Side.Down] )
            ) +
            Mathf.Max(
                ScorePatternRuleSide( pos, side, directions[( int )Side.Right] ),
                ScorePatternRuleSide( pos, side, directions[( int )Side.Left] )
            ) );
    }
}
