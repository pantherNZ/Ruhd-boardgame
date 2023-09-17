using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : EventReceiverInstance
{
    [SerializeField] ScoresHandler scoresHandlerRef;
    [SerializeField] TMPro.TextMeshProUGUI gameOverText;
    [SerializeField] VerticalLayoutGroup resultsLayoutgroupRef;
    [SerializeField] GameObject resultsPanelRef;
    [SerializeField] GameObject playerResultPrefab;
    [SerializeField] CanvasGroup showPanelButtonRef;
    [SerializeField] float panelScoresDelaySec = 1.0f;
    [SerializeField] float panelInterpXOffset = -250.0f;
    [SerializeField] float panelInterpTimeSec = 0.5f;
    [SerializeField] float panelFadeTimeSec = 0.5f;
    [SerializeField] float resultStartScale = 5.0f;

    protected override void Start()
    {
        base.Start();

        showPanelButtonRef.SetVisibility( false );
    }
    
    public void TogglePanel()
    {
        resultsPanelRef.ToggleActive();

        if( resultsPanelRef.activeSelf )
        {
            this.FadeFromTransparent( resultsPanelRef, panelFadeTimeSec, Utility.Easing.Quintic.In );
            resultsPanelRef.transform.localPosition = new Vector3( panelInterpXOffset, 0.0f, 0.0f );
            this.InterpolatePosition( resultsPanelRef.transform, Vector3.zero, panelInterpTimeSec, true, Utility.Easing.Quintic.In );
            resultsPanelRef.transform.localScale = new Vector3( 0.0f, 1.0f, 1.0f );
            this.InterpolateScale( resultsPanelRef.transform, Vector3.one, panelInterpTimeSec, Utility.Easing.Quintic.In );
            if( showPanelButtonRef.IsVisible() )
                this.FadeToTransparent( showPanelButtonRef, panelInterpTimeSec, Utility.Easing.Quintic.Out );
        }
        else
        {
            this.FadeToTransparent( resultsPanelRef, panelFadeTimeSec, Utility.Easing.Quintic.In, true );
            this.InterpolatePosition( resultsPanelRef.transform, new Vector3( panelInterpXOffset, 0.0f, 0.0f ), panelInterpTimeSec, true, Utility.Easing.Quintic.In );
            this.InterpolateScale( resultsPanelRef.transform, new Vector3( 0.0f, 1.0f, 1.0f ), panelInterpTimeSec, Utility.Easing.Quintic.In );
            this.FadeFromTransparent( showPanelButtonRef, panelInterpTimeSec, Utility.Easing.Quintic.In );
        }
    }

    public void ExitGame()
    {

    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is GameOverEvent )
        {
            TogglePanel();

            var scores = scoresHandlerRef.CurrentScores.ToList();
            scores.Sort( ( x, y ) => x.score - y.score );
            Utility.FunctionTimer.CreateTimer( panelScoresDelaySec, () =>
            {
                foreach( var (idx, score) in scores.Enumerate() )
                    StartCoroutine( ShowResult( score, 1.0f - idx * 0.1f ) );
            } );
        }
        else if( e is StartGameEvent )
        {
            resultsPanelRef.GetComponent<CanvasGroup>().SetVisibility( false );
            resultsPanelRef.SetActive( false );
            resultsLayoutgroupRef.transform.DestroyChildren();
        }
    }

    private IEnumerator ShowResult( ScoresHandler.BasePlayerEntry score, float scale )
    {
        var resultDisplay = Instantiate( playerResultPrefab, resultsLayoutgroupRef.transform );
        resultDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = score.name.ToString();
        resultDisplay.GetComponentInChildren<TextNumberAnimatorGroupUI>().SetValue( score.score );
        resultDisplay.transform.localScale = new Vector3( resultStartScale, resultStartScale, resultStartScale );
        yield return Utility.InterpolateScale( resultDisplay.transform, Vector3.one, scale, Utility.Easing.Quadratic.In );
        //this.Shake( Camera.main.transform, 0.3f, 18.0f, 3.0f, 30.0f, 2.0f );
    }
}
