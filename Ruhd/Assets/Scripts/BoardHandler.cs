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
    Vector2Int boardSize;

    protected override void Start()
    {
        base.Start();

        boardSize = new Vector2Int(
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
        if( pos.x >= boardSize.x / 2 ||
            pos.y >= boardSize.y / 2 ||
            pos.x < -boardSize.x / 2 ||
            pos.y < -boardSize.y / 2 )
            return false;
        if( board.ContainsKey( pos ) )
            return false;
        return board.ContainsKey( pos + new Vector2Int( 1, 0 ) ) ||
               board.ContainsKey( pos + new Vector2Int( -1, 0 ) ) ||
               board.ContainsKey( pos + new Vector2Int( 0, 1 ) ) ||
               board.ContainsKey( pos + new Vector2Int( 0, -1 ) );
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
        {
            HighlightAvailableSpot( pos + new Vector2Int( 1, 0 ) );
            HighlightAvailableSpot( pos + new Vector2Int( -1, 0 ) );
            HighlightAvailableSpot( pos + new Vector2Int( 0, 1 ) );
            HighlightAvailableSpot( pos + new Vector2Int( 0, -1 ) );
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

        if( newCard != null )
            board.Remove( pos );

        return score;
    }

    private CardSide? GetCurrentSide( Vector2Int pos, Side direction )
    {
        return direction switch
        {
            Side.Up => GetAdjacentSide( pos + new Vector2Int( 0, 1 ), Side.Down ),
            Side.Right => GetAdjacentSide( pos + new Vector2Int( 1, 0 ), Side.Left ),
            Side.Down => GetAdjacentSide( pos + new Vector2Int( 0, -1 ), Side.Up ),
            Side.Left => GetAdjacentSide( pos + new Vector2Int( -1, 0 ), Side.Right ),
            _ => null,
        };
    }

    private CardSide? GetAdjacentSide( Vector2Int pos, Side direction )
    {
        switch( direction )
        {
            case Side.Up:
                if( board.TryGetValue( pos + new Vector2Int( 0, 1 ), out var value ) )
                    return value.data.sides[( int )value.rotation % 2 == 0 ? 2 - ( int )value.rotation : ( int )value.rotation];
                break;
            case Side.Right:
                if( board.TryGetValue( pos + new Vector2Int( 1, 0 ), out value ) )
                    return value.data.sides[3 - ( int )value.rotation];
                break;
            case Side.Down:
                if( board.TryGetValue( pos + new Vector2Int( 0, -1 ), out value ) )
                    return value.data.sides[( int )value.rotation];
                break;
            case Side.Left:
                if( board.TryGetValue( pos + new Vector2Int( -1, 0 ), out value ) )
                    return value.data.sides[( int )value.rotation + ( ( int )value.rotation % 2 == 0 ? 1 : -1 )];
                break;
        }

        return null;
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
                ScorePatternRuleSide( pos, side, new Vector2Int( 1, 0 ) ),
                ScorePatternRuleSide( pos, side, new Vector2Int( -1, 0 ) )
            ) +
            Mathf.Max(
                ScorePatternRuleSide( pos, side, new Vector2Int( 0, 1 ) ),
                ScorePatternRuleSide( pos, side, new Vector2Int( 0, -1 ) )
            ) );
    }
}
