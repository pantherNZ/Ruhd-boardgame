using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuTileUI : MonoBehaviour
{
    [SerializeField] CanvasGroup mainCanvas;
    [SerializeField] CanvasGroup alternativeCanvas;
    [SerializeField] RectTransform centreMenuArea;
    [SerializeField] float fadeTimeSec = 0.5f;

    private bool centreHighlight;
    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;

    public void ToggleFadeText( bool showPlayGame )
    {
        mainCanvas.gameObject.SetActive( true );
        alternativeCanvas.gameObject.SetActive( true );

        var fadeIn = showPlayGame ? alternativeCanvas : mainCanvas;
        var fadeOut = showPlayGame ? mainCanvas : alternativeCanvas;

        if( fadeInCoroutine != null )
        {
            StopCoroutine( fadeInCoroutine );
            StopCoroutine( fadeOutCoroutine );
        }

        fadeInCoroutine = StartCoroutine( Utility.FadeFromBlack( fadeIn, fadeTimeSec ) );
        fadeOutCoroutine = StartCoroutine( Utility.FadeToBlack( fadeOut, fadeTimeSec ) );
    }

    public void StartGame()
    {
        SceneManager.LoadSceneAsync( "GameScene", LoadSceneMode.Additive );
    }

    private void Update()
    {
        if( centreMenuArea != null && centreHighlight != centreMenuArea.GetSceenSpaceRect().Contains( Utility.GetMouseOrTouchPos() ) )
        {
            centreHighlight = !centreHighlight;
            ToggleFadeText( centreHighlight );
        }
    }
}
