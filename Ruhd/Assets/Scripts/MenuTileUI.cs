using UnityEngine;

public class MenuTileUI : MonoBehaviour
{
    [SerializeField] CanvasGroup mainCanvas;
    [SerializeField] CanvasGroup alternativeCanvas;
    [SerializeField] float fadeTimeSec = 0.5f;

    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;

    public void ToggleFadeText( bool showPlayGame )
    {
        mainCanvas.gameObject.SetActive( true );
        alternativeCanvas.gameObject.SetActive( true );

        var fadeIn = showPlayGame ? alternativeCanvas : mainCanvas;
        var fadeOut = showPlayGame ? mainCanvas : alternativeCanvas;

        if( fadeInCoroutine != null )
            StopCoroutine( fadeInCoroutine );

        if( fadeOutCoroutine != null )
            StopCoroutine( fadeOutCoroutine );

        fadeInCoroutine = StartCoroutine( Utility.FadeFromBlack( fadeIn, fadeTimeSec ) );
        fadeOutCoroutine = StartCoroutine( Utility.FadeToBlack( fadeOut, fadeTimeSec ) );
    }
}
