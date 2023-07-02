using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2Int gridSize;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] int flippedChancePercent;
    [SerializeField] int howToPlayChancePercent;
    [SerializeField] GameObject playTilePrefab;
    [SerializeField] GameObject howToPlayTilePrefab;

    private void Start()
    {
        var allTiles = new List<TileComponent>();

        for( int y = -gridSize.y / 2; y < gridSize.y / 2; ++y )
        {
            for( int x = -gridSize.x / 2; x < gridSize.x / 2; ++x )
            {
                if( y >= -1 && y < 1 && x >= -2 && x < 2 )
                    continue;

                var tile = deck.DrawTile( true );
                allTiles.Add( tile );
                tile.transform.SetParent( background.transform );
                ( tile.transform as RectTransform ).anchorMin = new Vector2( 0.5f, 0.5f );
                ( tile.transform as RectTransform ).anchorMax = new Vector2( 0.5f, 0.5f );
                ( tile.transform as RectTransform ).anchoredPosition = GetPosition( new Vector2Int( x, y ) );

                if( deck.IsDeckEmpty() )
                    deck.Reset();

                if( y >= -2 && y < 2 && x >= -3 && x < 3 )
                    continue;

                if( Random.Range( 0, 100 ) < flippedChancePercent )
                    ReplaceTile( tile.gameObject, playTilePrefab, true );

                if( Random.Range( 0, 100 ) < howToPlayChancePercent )
                    ReplaceTile( tile.gameObject, howToPlayTilePrefab, true );
            }
        }

        ReplaceTile( allTiles.RandomItem().gameObject, howToPlayTilePrefab, true );
    }

    private void ReplaceTile( GameObject replacee, GameObject prefab, bool resetRotation )
    {
        var replacement = Instantiate( prefab, transform );
        replacement.transform.Match( replacee.transform );
        if( resetRotation )
            replacement.transform.rotation = Quaternion.identity;
        replacee.Destroy();
    }

    private Vector2 GetPosition( Vector2Int pos )
    {
        return cellSize / 2.0f + new Vector2(
            pos.x * ( cellSize.x + padding.x ),
            pos.y * ( cellSize.y + padding.y ) );
    }
}
