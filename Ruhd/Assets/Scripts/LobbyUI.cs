using System.Collections;
using System.Collections.Generic;
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
            codeLabel.text = "CODE: <voffset=-0.2em><size=40>" + lobbyUpdated.lobby.LobbyCode;
            playersLabel.text = string.Join( "\n", lobbyUpdated.lobby.Players.Select( x => x.Data["PlayerName"] ) );
        }
    }
}
