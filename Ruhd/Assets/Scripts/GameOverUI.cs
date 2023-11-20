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
    [SerializeField] float panelPerScoreDelaySec = 0.5f;
    [SerializeField] float panelInterpXOffset = -250.0f;
    [SerializeField] float panelInterpTimeSec = 0.5f;
    [SerializeField] float panelFadeTimeSec = 0.5f;
    [SerializeField] float resultStartScale = 5.0f;
    [SerializeField] float resultScaleInterpTimeSec = 1.0f;

    private bool gameOver;

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
        EventSystem.Instance.TriggerEvent( new ExitGameEvent() { fromGameOver = true } );
        showPanelButtonRef.SetVisibility( false );
        resultsPanelRef.GetComponent<CanvasGroup>().SetVisibility( false );
        resultsPanelRef.SetActive( false );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is GameOverEvent )
        {
            gameOver = true;
        }
        else if( e is StartGameEvent )
        {
            gameOver = false;
            resultsPanelRef.GetComponent<CanvasGroup>().SetVisibility( false );
            resultsPanelRef.SetActive( false );
            resultsLayoutgroupRef.transform.DestroyChildren();
        }
        else if( e is PlayerScoreEvent && gameOver )
        {
            Utility.FunctionTimer.CreateTimer( 3.0f, () =>
            {
                TogglePanel();

                var scores = scoresHandlerRef.CurrentScores.ToList();
                scores.Sort( ( x, y ) => y.score - x.score );
                foreach( var (idx, score) in scores.Enumerate() )
                {
                    Utility.FunctionTimer.CreateTimer( panelScoresDelaySec + idx * panelPerScoreDelaySec, () =>
                    {
                        StartCoroutine( ShowResult( score, idx == 0 ? 1.0f : 0.9f ) );
                    } );
                }
            } );
        }
    }

    private IEnumerator ShowResult( ScoresHandler.BasePlayerEntry score, float scale )
    {
        var resultDisplay = Instantiate( playerResultPrefab, resultsLayoutgroupRef.transform );
        resultDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"{(score.disconnected ? "<s>" : string.Empty)}{score.name}{(score.disconnected ? "</s>" : string.Empty)}";
        resultDisplay.GetComponentInChildren<TextNumberAnimatorGroupUI>().SetValue( score.score );
        var group = resultDisplay.GetComponent<CanvasGroup>();
        group.SetVisibility( false );
        this.FadeFromTransparent( group, resultScaleInterpTimeSec );
        resultDisplay.transform.localScale = new Vector3( resultStartScale, resultStartScale, resultStartScale );
        yield return Utility.InterpolateScale( resultDisplay.transform, new Vector3( scale, scale, scale ), resultScaleInterpTimeSec, Utility.Easing.Quadratic.In );
        this.Shake( Camera.main.transform, 0.3f, -0.015f, 0.001f, 60.0f, 1.2f );
    }
}
