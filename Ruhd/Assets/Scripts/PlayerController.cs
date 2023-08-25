using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    private NetworkHandler networkHandler;
    private FixedString128Bytes[] playerNamesServerOnly;
    private int playerNamesIdxServerOnly;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkHandler = NetworkManager.Singleton.GetComponent<NetworkHandler>();

        if( IsServer )
            playerNamesServerOnly = new FixedString128Bytes[NetworkManager.Singleton.ConnectedClients.Count];

        if( IsOwner )
            SetPlayerNameServerRpc( networkHandler.localPlayerName );
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        playerNamesServerOnly[playerNamesIdxServerOnly] = new FixedString128Bytes( name );
        ++playerNamesIdxServerOnly;

        if( playerNamesIdxServerOnly == NetworkManager.Singleton.ConnectedClients.Count )
        {
            var playersReadyEvent = new PlayersReadyEvent()
            {
                playerNames = playerNamesServerOnly
            };
            TriggerPlayersReadyEventClientRpc( playersReadyEvent );
        }
    }

    [ClientRpc]
    private void TriggerPlayersReadyEventClientRpc( PlayersReadyEvent playersReadyEvent )
    {
        EventSystem.Instance.TriggerEvent( playersReadyEvent );
    }
}