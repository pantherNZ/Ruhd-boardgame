using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeUI : EventReceiverInstance
{
    [SerializeField] GameObject timerDisplay;
    [SerializeField] Button challengeBtn;
    [SerializeField] BoardHandler boardHandler;
    private TMPro.TextMeshProUGUI timer;

    protected override void Start()
    {
        base.Start();

        timer = timerDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        timerDisplay.SetActive( false );
        challengeBtn.gameObject.SetActive( false );

        challengeBtn.onClick.RemoveAllListeners();
        challengeBtn.onClick.AddListener( () =>
        {
            boardHandler.RequestChallenge( null );
        } );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TilePlacedEvent tilePlaced )
        {
            if( GameController.Instance.isLocalPlayerTurn )
                return;

            timerDisplay.SetActive( tilePlaced.waitingForChallenge );
            challengeBtn.gameObject.SetActive( tilePlaced.waitingForChallenge );

            if( tilePlaced.waitingForChallenge )
                StartCoroutine( UpdateTimerText( GameConstants.Instance.challengeStartTimerSec ) );
        }
        else if( e is ChallengeStartedEvent challengeStarted )
        {
            // TODO: Show challenge activated UI

            timerDisplay.SetActive( true );
            challengeBtn.gameObject.SetActive( false );
            StopAllCoroutines();
            StartCoroutine( UpdateTimerText( GameConstants.Instance.challengeStartTimerSec ) );
        }
    }

    private IEnumerator UpdateTimerText( float duration )
    {
        while( duration > 0.0f )
        {
            timer.text = duration.ToString();
            duration -= 1.0f;
            yield return new WaitForSeconds( 1.0f );
        }
    }
}
