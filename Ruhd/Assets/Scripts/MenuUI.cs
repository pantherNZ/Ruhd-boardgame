using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2Int gridSize;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] int flippedChancePercent;
    [SerializeField] TMPro.TextMeshProUGUI titleText;
    [SerializeField] TMPro.TextMeshProUGUI playText;
    [SerializeField] float fadeTimeSec = 0.5f;

    private CanvasGroup titleCanvas;
    private CanvasGroup playCanvas;
    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;

    private void Start()
    {
        titleCanvas = titleText.GetComponent<CanvasGroup>();
        playCanvas = playText.GetComponent<CanvasGroup>();

        for( int y = -gridSize.y / 2; y < gridSize.y / 2; ++y )
        {
            for( int x = -gridSize.x / 2; x < gridSize.x / 2; ++x )
            {
                if( y >= -1 && y < 1 && x >= -2 && x < 2 )
                    continue;

                var tile = deck.DrawTile( true );
                tile.transform.SetParent( transform );
                tile.transform.localPosition = GetPosition( new Vector2Int( x, y ) );

                if( Random.Range( 0, 100 ) < flippedChancePercent )
                    tile.ShowBack();

                if( deck.IsDeckEmpty() )
                    deck.Reset();
            }
        }
    }

    private Vector2 GetPosition( Vector2Int pos )
    {
        return new Vector2(
            pos.x * ( cellSize.x + padding.x ),
            pos.y * ( cellSize.y + padding.y ) ) + cellSize / 2.0f;
    }

    public void ToggleFadeText( bool showPlayGame )
    {
        titleText.gameObject.SetActive( true );
        playText.gameObject.SetActive( true );

        var fadeIn = showPlayGame ? playCanvas : titleCanvas;
        var fadeOut = showPlayGame ? titleCanvas : playCanvas;

        if( fadeInCoroutine != null )
        {
            StopCoroutine( fadeInCoroutine );
            StopCoroutine( fadeOutCoroutine );
        }

        fadeInCoroutine = StartCoroutine( Utility.FadeFromBlack( fadeIn, fadeTimeSec ) );
        fadeOutCoroutine = StartCoroutine( Utility.FadeToBlack( fadeOut, fadeTimeSec ) );

        Debug.Log( showPlayGame.ToString() );
    }
}
