using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    Dictionary<Vector2Int, CardComponent> board;
    
    public int EvaluateScore( Vector2Int pos, CardComponent newCard )
    {
        return ScoreOneSideRule( pos, newCard ) +
            ScoreDiffRule( pos, newCard ) +
            ScoreSameRule( pos, newCard ) +
            ScorePatternRule( pos, newCard );
    }

    CardSide? GetAdjacentCard( Vector2Int pos, Side direction )
    {
        switch( direction )
        {
            case Side.Up:
                if( board.TryGetValue( pos + new Vector2Int( 0, 1 ), out var value ) )
                    return value.data.sides[( int )Side.Down];
                break;
            case Side.Right:
                if( board.TryGetValue( pos + new Vector2Int( 1, 0 ), out value ) )
                    return value.data.sides[( int )Side.Left];
                break;
            case Side.Down:
                if( board.TryGetValue( pos + new Vector2Int( 0, -1 ), out value ) )
                    return value.data.sides[( int )Side.Up];
                break;
            case Side.Left:
                if( board.TryGetValue( pos + new Vector2Int( -1, 0 ), out value ) )
                    return value.data.sides[( int )Side.Right];
                break;
        }

        return null;
    }

    int ScoreOneSideRule( Vector2Int pos, CardComponent _ )
    {
        int oneSideExtraScore = 1;

        int sides = 0;
        sides += GetAdjacentCard( pos, Side.Up ) != null ? 1 : 0;
        sides += GetAdjacentCard( pos, Side.Right ) != null ? 1 : 0;
        sides += GetAdjacentCard( pos, Side.Down ) != null ? 1 : 0;
        sides += GetAdjacentCard( pos, Side.Left ) != null ? 1 : 0;
        return sides == 1 ? oneSideExtraScore : 0;
    }

    int ScoreDiffRule( Vector2Int pos, CardComponent newCard )
    {
        var side = GetAdjacentCard( pos, Side.Up );
        if( side != null && side.Value.colour != newCard.data.sides[( int )Side.Up].colour )
            return Mathf.Abs( side.Value.value - newCard.data.sides[( int )Side.Up].value );
        return 0;
    }

    int ScoreSameRule( Vector2Int pos, CardComponent newCard )
    {
        return 0;
    }

    int ScorePatternRule( Vector2Int pos, CardComponent newCard )
    {
        return 0;
    }
}
