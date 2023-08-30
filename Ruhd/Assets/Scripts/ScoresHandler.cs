using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    private List<PlayerEntry> players;

    protected override void Start()
    {
        base.Start();
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent startGame )
        {
            InitPlayers( startGame.playerNames );
        }
        else if( e is PlayerScoreEvent scoreEvent )
        {
            var player = players.Find( x => x.playerIdx == scoreEvent.playerIdx );
            player.score += scoreEvent.scoreModifier;
            player.scoreText.text = players[scoreEvent.playerIdx].score.ToString();
        }
    }

    public void InitPlayers(List<string> playerNames )
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

        foreach( var( idx, player ) in playerNames.Enumerate() )
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
                name = player,
                score = 0,
                playerIdx = idx,
                nameText = nameText.GetComponent<TMPro.TextMeshProUGUI>(),
                scoreText = scoreText.GetComponent<TMPro.TextMeshProUGUI>(),
            };

            this.players.Add( playerEntry );
            playerEntry.nameText.text = player;
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
