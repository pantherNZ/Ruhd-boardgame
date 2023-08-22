using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuVideoTileUI : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool playingVideo;

    private void Start()
    {
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
}
