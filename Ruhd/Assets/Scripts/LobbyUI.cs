using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : EventReceiverInstance
{
    [SerializeField] TMPro.TextMeshProUGUI codeLabel;
    [SerializeField] TMPro.TextMeshProUGUI playersLabel;
    [SerializeField] TMPro.TextMeshProUGUI playersDataLabel;
    [SerializeField] TMPro.TextMeshProUGUI playersCountLabel;
    [SerializeField] Button startGameBtn;
    [SerializeField] Button toggleReadyBtn;

    private TMPro.TextMeshProUGUI toggleReadyLabel;
    private bool localIsReady = false;

    protected override void Start()
    {
        base.Start();

        base.modifyListenerWithEnableDisable = false;
        toggleReadyLabel = toggleReadyBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        toggleReadyBtn.onClick.AddListener( () =>
        {
            localIsReady = !localIsReady;
            toggleReadyLabel.text = localIsReady ? "UNREADY" : "READY";
        } );
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is LobbyUpdatedEvent lobbyUpdated )
        {
            bool multiplePlayers = lobbyUpdated.playerData.Count > 1;
            codeLabel.text = lobbyUpdated.lobby.LobbyCode;
            var playersList = new StringBuilder();
            playersList.AppendJoin( '\n', lobbyUpdated.playerData.Select( x => x.name ) );
            if( !multiplePlayers )
                playersList.Append( "\n\nWAITING FOR OPPONENTS..." );
            playersLabel.text = playersList.ToString();
            playersCountLabel.text = $"{lobbyUpdated.playerData.Count}/{4}";
            playersDataLabel.text = string.Join( "\n", lobbyUpdated.playerData.Select( x => x.isReady ? "READY" : "NOT READY" ) );

            bool canStartGame = lobbyUpdated.lobby.HostId == AuthenticationService.Instance.PlayerId &&
                multiplePlayers &&
                lobbyUpdated.playerData.All( x => x.isReady );
            startGameBtn.gameObject.SetActive( canStartGame );
        }
    }
}
