using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] int flippedChancePercent;
    [SerializeField] int howToPlayChancePercent;
    [SerializeField] GameObject playTilePrefab;
    [SerializeField] GameObject howToPlayTilePrefab;
    Vector2Int gridSize;

    private void Start()
    {
        var allTiles = new List<TileComponent>();

        var cameraRect = Camera.main.pixelRect;
        cameraRect.center = new Vector2( 0, 0 );

        var expandedRect = cameraRect;
        expandedRect.size += cellSize * 2.0f;
        expandedRect.center = new Vector2( 0, 0 );

        gridSize = new Vector2Int(
            Mathf.RoundToInt( expandedRect.width / cellSize.x ),
            Mathf.RoundToInt( expandedRect.height / cellSize.y ) );

        for( int y = -gridSize.y / 2; y < gridSize.y / 2; ++y )
        {
            for( int x = -gridSize.x / 2; x < gridSize.x / 2; ++x )
            {
                if( y >= -1 && y < 1 && x >= -2 && x < 2 )
                    continue;

                var newPosition = GetPosition( new Vector2Int( x, y ) );
                var tile = deck.DrawTile( true );
                var isOutsideCamera = !cameraRect.Contains( newPosition );
                if( !isOutsideCamera )
                    allTiles.Add( tile );
                tile.transform.SetParent( background.transform, false );
                var rectTransform = tile.transform as RectTransform;
                rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
                rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
                rectTransform.anchoredPosition = newPosition;

                if( deck.IsDeckEmpty() )
                    deck.Reset();

                if( y >= -2 && y < 2 && x >= -3 && x < 3 )
                    continue;

                if( isOutsideCamera )
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
        ( replacement.transform as RectTransform ).anchorMin = new Vector2( 0.5f, 0.5f );
        ( replacement.transform as RectTransform ).anchorMax = new Vector2( 0.5f, 0.5f );
        replacement.transform.Match( replacee.transform );
        if( resetRotation )
            replacement.transform.rotation = Quaternion.identity;
        replacee.Destroy();
    }

    private Vector2 GetPosition( Vector2Int pos )
    {
        return cellSize / 2.0f + padding / 2.0f + new Vector2(
            pos.x * ( cellSize.x + padding.x ),
            pos.y * ( cellSize.y + padding.y ) );
    }
}
