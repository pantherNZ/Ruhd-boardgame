using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoresHandler : EventReceiverInstance
{
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
            var player = players[playerIdx];
            player.score += scoreEvent.scoreModifier;
            player.scoreText.text = player.score.ToString();

            StartCoroutine( CreateScoreDisplayUI( scoreEvent.tile, playerIdx ) );
        }
    }

    private IEnumerator CreateScoreDisplayUI( TileComponent tile, int playerIdx )
    {
        var scoreDisplay = Instantiate( scoreGainedUIPrefab, transform );
        scoreDisplay.transform.localPosition = transform.InverseTransformPoint( tile.transform.position );
        yield return Utility.InterpolatePosition( scoreDisplay.transform, scoreDisplay.transform.localPosition + new Vector3( 0.0f, 200.0f, 0.0f ), 2.0f, true, false );
        const float textLineHeight = 50.0f;
        var scoreBoardPos = ( scoreValues.transform as RectTransform ).rect.TopRight().ToVector3();
        yield return Utility.InterpolatePosition( scoreDisplay.transform, scoreBoardPos - new Vector3( 0.0f, textLineHeight * ( playerIdx + 1 ), 0.0f ), 1.0f, true, false );
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
