using System.Linq;
using UnityEngine;

public class LobbyUI : EventReceiverInstance
{
    [SerializeField] TMPro.TextMeshProUGUI codeLabel;
    [SerializeField] TMPro.TextMeshProUGUI playersLabel;

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is LobbyUpdatedEvent lobbyUpdated )
        {
            codeLabel.text = lobbyUpdated.lobby.LobbyCode;
            playersLabel.text = string.Join( "\n", lobbyUpdated.playerNames );
        }
    }
}
