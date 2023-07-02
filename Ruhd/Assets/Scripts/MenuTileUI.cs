using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

enum MenuTileType
{

}

public class MenuTileUI : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI baseText;
    [SerializeField] TMPro.TextMeshProUGUI alternativeText;
    [SerializeField] float fadeTimeSec = 0.5f;
    [SerializeField] Color highlightTextColour;

    private VideoPlayer videoPlayer;

    private CanvasGroup titleCanvas;
    private CanvasGroup playCanvas;
    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;
    private bool playingVideo;

    private void Start()
    {
        titleCanvas = baseText.GetComponent<CanvasGroup>();
        playCanvas = alternativeText.GetComponent<CanvasGroup>();

        videoPlayer = FindObjectOfType<VideoPlayer>( true );
    }

    private void Update()
    {
        if( playingVideo && videoPlayer.time > 1.0f )
            if( Input.anyKeyDown )
                VideoPlayerFinished( videoPlayer );
    }

    public void ShowVideo()
    {
        videoPlayer.gameObject.SetActive( true );
        videoPlayer.Play();
        videoPlayer.transform.SetAsLastSibling();
        playingVideo = true;
        videoPlayer.loopPointReached += VideoPlayerFinished;
    }

    private void VideoPlayerFinished( VideoPlayer source )
    {
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive( false );
        playingVideo = false;
        videoPlayer.loopPointReached -= VideoPlayerFinished;
    }

    public void ToggleFadeText( bool showPlayGame )
    {
        baseText.gameObject.SetActive( true );
        alternativeText.gameObject.SetActive( true );

        var fadeIn = showPlayGame ? playCanvas : titleCanvas;
        var fadeOut = showPlayGame ? titleCanvas : playCanvas;

        if( fadeInCoroutine != null )
        {
            StopCoroutine( fadeInCoroutine );
            StopCoroutine( fadeOutCoroutine );
        }

        fadeInCoroutine = StartCoroutine( Utility.FadeFromBlack( fadeIn, fadeTimeSec ) );
        fadeOutCoroutine = StartCoroutine( Utility.FadeToBlack( fadeOut, fadeTimeSec ) );
    }

    public void HighlightTile()
    {
        alternativeText.color = highlightTextColour;
    }

    public void UnighlightTile()
    {
        alternativeText.color = Color.white;
    }

    public void StartGame()
    {
        SceneManager.LoadSceneAsync( "GameScene", LoadSceneMode.Additive );
    }
}
