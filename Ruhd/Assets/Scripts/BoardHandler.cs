using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    const int oneSideExtraScore = 1;
    const int patternExtraScore = 2;
    Dictionary<Vector2Int, CardComponent> board;
    
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

    CardSide? GetCurrentSide( Vector2Int pos, Side direction )
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

    CardSide? GetAdjacentSide( Vector2Int pos, Side direction )
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
                    return value.data.sides[( int )value.rotation + ( int )value.rotation % 2 == 0 ? 1 : -1];
                break;
        }

        return null;
    }

    void GetOpposingCardSides( Vector2Int pos, Side direction, out CardSide? side, out CardSide? other )
    {
        side = GetAdjacentSide( pos, direction );
        other = GetCurrentSide( pos, direction );
    }

    int ScoreOneSideRule( Vector2Int pos )
    {
        int sides = Utility.GetEnumValues<Side>().Sum( side => GetAdjacentSide( pos, side ) != null ? 1 : 0 );
        return sides == 1 ? oneSideExtraScore : 0;
    }

    int ScoreDiffRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( other != null && side.Value.colour != other.Value.colour )
            return Mathf.Abs( side.Value.value - other.Value.value );
        return 0;
    }

    int ScoreDiffRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreDiffRuleSide( pos, side ) );
    }

    int ScoreSameRuleSide( Vector2Int pos, Side direction )
    {
        GetOpposingCardSides( pos, direction, out var side, out var other );
        if( other != null && side.Value.colour == other.Value.colour && side.Value.value == other.Value.value )
            return side.Value.value;
        return 0;
    }

    int ScoreSameRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => ScoreSameRuleSide( pos, side ) );
    }

    int ScorePatternRuleSide( Vector2Int pos, Side direction, bool positive )
    {
        var curLeft = GetCurrentSide( pos, direction );
        var nextLeft = GetCurrentSide( pos + new Vector2Int( positive ? 1 : -1, 0 ), direction );
        var nextNextLeft = GetCurrentSide( pos + new Vector2Int( positive ? 2 : -2, 0 ), direction );
        return curLeft.HasValue && nextLeft.HasValue && nextNextLeft.HasValue &&
            (
                ( curLeft.Value.colour == nextLeft.Value.colour && curLeft.Value.colour == nextNextLeft.Value.colour ) ||
                ( curLeft.Value.value == nextLeft.Value.value && curLeft.Value.value == nextNextLeft.Value.value )
            ) ? patternExtraScore : 0;
    }

    int ScorePatternRule( Vector2Int pos )
    {
        return Utility.GetEnumValues<Side>().Sum( side => 
            ScorePatternRuleSide( pos, side, true ) + 
            ScorePatternRuleSide( pos, side, false ) );
    }
}
