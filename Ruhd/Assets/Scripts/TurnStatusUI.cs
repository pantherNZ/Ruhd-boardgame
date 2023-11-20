using UnityEngine;

public class TurnStatusUI : EventReceiverInstance
{
    [SerializeField] TMPro.TextMeshProUGUI label;
    private bool gameOver;

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent )
        {
            gameOver = false;
        }
        else if( e is TilePlacedEvent tilePlaced && !gameOver )
        {
            if( GameController.Instance.isLocalPlayerTurn )
            {
                if( tilePlaced.successfullyPlaced )
                    label.text = "PLACE A TILE";
                else if( tilePlaced.waitingForChallenge )
                    label.text = "OPEN TO CHALLENGE";
            }
        }
        else if( e is ChallengeStartedEvent )
        {
            label.text = "CHALLENGE";
        }
        else if( e is TurnStartEvent turnStart && !gameOver )
        {
            if( GameController.Instance.isLocalPlayerTurn )
            {
                label.text = "PLACE A TILE";
            }
            else
            {
                label.text = $"{turnStart.player}'S TURN";
            }
        }
        else if ( e is GameOverEvent )
        {
            gameOver = true;
            Utility.FunctionTimer.CreateTimer( 3.0f, () =>
            {
                label.text = "GAME OVER";
            } );
        }
    }
}
