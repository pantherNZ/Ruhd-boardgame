using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScoreSource
{
    MatchingSide,
    SideDifference,
    SingleSideBonus,
    Pattern
}

public class ScoreInfo
{
    public ScoreSource source;
    public int score;
    public List<TileSide> sides;
}

public class ScoresHandler : EventReceiverInstance
{
    public static string[] ScoreSourceNames = new string[]
    {
        "MATCHING SIDE",
        "SIDE DIFFERENCE",
        "SINGLE SIDE BONUS",
        "MATCHING PATTERN",
    };

    class PlayerEntry
    {
        public string name;
        public int score;
        public int playerIdx;
        public TMPro.TextMeshProUGUI nameText;
        public TMPro.TextMeshProUGUI scoreText;
    }

    [SerializeField] Transform scoreNames;
    [SerializeField] Transform scoreValues;
    [SerializeField] GameObject scoreGainedUIPrefab;
    [SerializeField] GameObject sideHighlightPrefab;
    private List<PlayerEntry> players;

    protected override void Start()
    {
        base.Start();
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent startGame )
        {
            InitPlayers( startGame.playerData );
        }
        else if( e is PlayerScoreEvent scoreEvent )
        {
            var playerIdx = players.FindIndex( x => x.name == scoreEvent.player );
            foreach( var (idx, scoreInfo) in scoreEvent.scoreModifiers.Enumerate() )
            {
                Utility.FunctionTimer.CreateTimer( 1.5f * idx, () =>
                {
                    StartCoroutine( CreateScoreDisplayUI( scoreInfo, playerIdx ) );
                } );
            }
        }
    }

    private void CreateSideHighlight( TileSide side )
    {
        var highlight = Instantiate( sideHighlightPrefab, side.card.owningComponent.transform );
        highlight.transform.localPosition = new Vector3();
        highlight.transform.localEulerAngles = new Vector3( 0.0f, 0.0f, side.side.Value() * -90.0f );
        Utility.FunctionTimer.CreateTimer( 1.0f, () => highlight.Destroy() );
    }

    private IEnumerator CreateScoreDisplayUI( ScoreInfo scoreModifier, int playerIdx )
    {
        yield return new WaitForSeconds( 0.2f );

        if( scoreModifier.sides.Count == 2 )
        {
            CreateSideHighlight( scoreModifier.sides[0] );
            CreateSideHighlight( scoreModifier.sides[1] );
        }
        else
        {
            foreach( var (idx, side) in scoreModifier.sides.Enumerate() )
                Utility.FunctionTimer.CreateTimer( 0.1f * idx, () => CreateSideHighlight( side ) );
        }

        var scoreDisplay = Instantiate( scoreGainedUIPrefab, transform );
        var text = scoreDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        text.text = $"+{scoreModifier.score} ({ScoreSourceNames[( int )scoreModifier.source]})";
        scoreDisplay.transform.localPosition = transform.InverseTransformPoint( scoreModifier.sides.Back().card.owningComponent.transform.position );
        yield return Utility.InterpolatePosition( scoreDisplay.transform, scoreDisplay.transform.localPosition + new Vector3( 0.0f, 200.0f, 0.0f ), 1.0f, true, Utility.Easing.Linear );
        const float textLineHeight = 50.0f;
        var scoreBoardPos = ( scoreValues.transform as RectTransform ).rect.TopRight().ToVector3();
        Utility.FunctionTimer.CreateTimer( 0.5f, () => this.FadeToColour( text, Color.clear, 0.5f, null, true ) );
        Utility.FunctionTimer.CreateTimer( 1.0f, () =>
        {
            var player = players[playerIdx];
            player.score += scoreModifier.score;
            player.scoreText.text = player.score.ToString();
        } );
        yield return Utility.InterpolatePosition( scoreDisplay.transform, scoreBoardPos - new Vector3( 0.0f, textLineHeight * ( playerIdx + 1 ), 0.0f ), 2.0f, true, ( x ) => Mathf.Min( 1.0f, Utility.Easing.Quintic.In( x + 0.5f ) - Utility.Easing.Quintic.In( 0.5f ) ) );
        scoreDisplay.Destroy();
    }

    public void InitPlayers(List<NetworkHandler.PlayerData> playerData )
    {
        if( players != null )
        {
            foreach( var (idx, player) in players.Enumerate() )
            {
                if( idx > 0 )
                {
                    player.nameText.DestroyObject();
                    player.scoreText.DestroyObject();
                }
            }
        }

        players = new List<PlayerEntry>();

        foreach( var( idx, player ) in playerData.Enumerate() )
        {
            var nameText = scoreNames.GetChild( 0 );
            var scoreText = scoreValues.GetChild( 0 );

            if( idx > 0 )
            {
                nameText = Instantiate( nameText, scoreNames.transform );
                scoreText = Instantiate( scoreText, scoreValues.transform );
            }

            var playerEntry = new PlayerEntry()
            {
                name = player.name,
                score = 0,
                playerIdx = idx,
                nameText = nameText.GetComponent<TMPro.TextMeshProUGUI>(),
                scoreText = scoreText.GetComponent<TMPro.TextMeshProUGUI>(),
            };

            this.players.Add( playerEntry );
            playerEntry.nameText.text = player.name;
            playerEntry.scoreText.text = "0";
        }
    }

    public void SortScores()
    {
        players.Sort( ( a, b ) =>
        {
            return b.score.CompareTo( a.score );
        } );

        foreach( var( idx, player ) in players.Enumerate() )
        {
            player.nameText.transform.SetSiblingIndex( idx );
            player.scoreText.transform.SetSiblingIndex( idx );
        }
    }
}
