using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public class BasePlayerEntry
    {
        public string name;
        public int score;
        public int playerIdx;
    }

    class PlayerEntry : BasePlayerEntry
    {
        public Transform instance;
        public TMPro.TextMeshProUGUI nameText;
        public TextNumberAnimatorGroupUI scoreText;
        public Image turnHighlight;
    }

    [SerializeField] Transform scoresList;
    [SerializeField] GameObject scoreGainedUIPrefab;
    [SerializeField] GameObject sideHighlightPrefab;
    private List<PlayerEntry> players;
    public IReadOnlyList<BasePlayerEntry> CurrentScores
    {
        get { return players.AsReadOnly(); }
    }

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
            SetTurnHighlight( scoreEvent.player );
            var playerIdx = players.FindIndex( x => x.name == scoreEvent.player );
            foreach( var (idx, scoreInfo) in scoreEvent.scoreModifiers.Enumerate() )
            {
                Utility.FunctionTimer.CreateTimer( 1.5f * idx, () =>
                {
                    StartCoroutine( CreateScoreDisplayUI( scoreInfo, playerIdx ) );
                } );
            }
        }
        else if( e is TurnStartEvent turnStartEvent )
        {
            SetTurnHighlight( turnStartEvent.player );
        }
    }

    private void SetTurnHighlight( string player )
    {
        var playerTurn = players.Find( x => x.name == player );
        foreach( var score in players )
            score.turnHighlight.gameObject.SetActive( false );
        playerTurn.turnHighlight.gameObject.SetActive( true );
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
        var scoreBoardPos = ( scoresList.transform as RectTransform ).rect.TopRight().ToVector3();
        Utility.FunctionTimer.CreateTimer( 0.5f, () => this.FadeToColour( text, Color.clear, 0.5f, null, true ) );
        Utility.FunctionTimer.CreateTimer( 1.0f, () =>
        {
            var player = players[playerIdx];
            player.score += scoreModifier.score;
            player.scoreText.SetValue( player.score );
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
                    player.instance.SetParent( null );
                    player.instance.gameObject.Destroy();
                }
            }
        }

        players = new List<PlayerEntry>();

        foreach( var( idx, player ) in playerData.Enumerate() )
        {
            var scoreInstance = scoresList.GetChild( 0 );

            if( idx > 0 )
                scoreInstance = Instantiate( scoreInstance, scoresList.transform );

            var playerEntry = new PlayerEntry()
            {
                name = player.name,
                score = 0,
                playerIdx = idx,
                instance = scoreInstance,
                nameText = scoreInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>(),
                scoreText = scoreInstance.GetComponentInChildren<TextNumberAnimatorGroupUI>(),
                turnHighlight = scoreInstance.GetComponentInChildren<Image>( true ),
            };

            this.players.Add( playerEntry );
            playerEntry.nameText.text = player.name;
            playerEntry.scoreText.SetValue( 0 );
        }
    }
}
