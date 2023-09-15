using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameOverUI : EventReceiverInstance
{
    [SerializeField] ScoresHandler scoresHandlerRef;
    [SerializeField] TMPro.TextMeshProUGUI gameOverText;
    [SerializeField] TMPro.TextMeshProUGUI playerResultPrefab;

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is GameOverEvent )
        {
            var scores = scoresHandlerRef.CurrentScores.ToList();
            scores.Sort( ( x, y ) => x.score - y.score );
            foreach( var (idx, score) in scores.Enumerate() )
                StartCoroutine( ShowResult( score ) );
        }
    }

    private IEnumerator ShowResult( ScoresHandler.BasePlayerEntry score )
    {
        var resultDisplay = Instantiate( playerResultPrefab );
        resultDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"{score.name} : {score.score}";
        resultDisplay.transform.localScale = new Vector3( 5.0f, 5.0f, 5.0f );
        yield return Utility.InterpolateScale( resultDisplay.transform, Vector3.one, 1.0f, Utility.Easing.Quadratic.In );
        this.Shake( Camera.main.transform, 0.3f, 18.0f, 3.0f, 30.0f, 2.0f );
    }
}
