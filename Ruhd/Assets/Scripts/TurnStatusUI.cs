using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnStatusUI : EventReceiverInstance
{
    [SerializeField] TMPro.TextMeshProUGUI label;
    private bool localPlayerTurn = false;

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is TilePlacedEvent tilePlaced )
        {
            if( localPlayerTurn && tilePlaced.successfullyPlaced )
                label.text = tilePlaced.tile.flipped ? "PLACE A TILE" : "FLIP A TILE";
        }
        else if( e is TurnStartEvent turnStart )
        {
            var localPlayerName = NetworkManager.Singleton.GetComponent<NetworkHandler>().localPlayerData.name;
            localPlayerTurn = turnStart.player == localPlayerName;

            if( localPlayerTurn )
            {
                label.text = "PLACE A TILE";
            }
            else
            {
                label.text = $"{turnStart.player}'S TURN";
            }
        }
    }
}
