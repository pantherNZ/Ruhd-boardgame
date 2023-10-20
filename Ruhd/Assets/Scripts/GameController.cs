using UnityEngine;
using System.Collections.Generic;

class GameController : EventReceiverInstance
{
    [SerializeField] AIController AIControllerPrefab;

    private List<AIController> AIPlayers = new List<AIController>();

    static GameController _Instance;
    static public GameController Instance
    {
        get => _Instance;
        private set { }
    }

    [HideInInspector] public int rngSeedRuntime;
    [HideInInspector] public Utility.SystemRandom gameRandom;
    [HideInInspector] public string currentPlayerTurn;
    [HideInInspector] public string localPlayerName;
    [HideInInspector] public string currentChallenger;
    [HideInInspector] public bool isLocalPlayerTurn { get => localPlayerName == currentPlayerTurn; private set { } }
    [HideInInspector] public bool isLocalPlayerChallenging { get => localPlayerName == currentChallenger; private set { } }
    [HideInInspector] public bool isOfflineGame = true;
    public BoardHandler boardHandler;
    public DeckHandler deckhandler;

    protected override void Start()
    {
        base.Start();

        _Instance = this;
    }

    public void InitRng( int? seedOverride = null )
    {
        var fixedSeed = GameConstants.Instance.rngSeed;
        rngSeedRuntime = seedOverride ?? ( fixedSeed == 0 ? Random.Range( 0, int.MaxValue ) : fixedSeed );
        gameRandom = new Utility.SystemRandom( rngSeedRuntime );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent startGame )
        {
            foreach( var player in startGame.playerData )
            {
                switch( player.type )
                {
                    case NetworkHandler.PlayerType.Local:
                        localPlayerName = player.name;
                        break;
                    case NetworkHandler.PlayerType.AI:
                        var ai = Instantiate( AIControllerPrefab );
                        ai.playerName = player.name;
                        ai.difficulty = 1.0f;
                        ai.deviation = 0.0f;
                        ai.board = boardHandler;
                        ai.deck = deckhandler;
                        AIPlayers.Add( ai );
                        break;
                    case NetworkHandler.PlayerType.Network:
                    {
                        if( !player.isRemotePlayer )
                            localPlayerName = player.name;
                        else
                            isOfflineGame = false;
                        break;
                    }
                };
            }
        }
        else if( e is TurnStartEvent turnStart )
        {
            currentPlayerTurn = turnStart.player;
        }
        else if( e is ChallengeStartedEvent challengeStarted )
        {
            currentChallenger = challengeStarted.player;
        } 
        else if( e is ExitGameEvent )
        {
            foreach( var x in AIPlayers )
                x.DestroyObject();
            AIPlayers.Clear();
        }
    }
}