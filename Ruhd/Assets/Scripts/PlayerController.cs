using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour, IEventReceiver
{
    public bool isPlayerTurn = false;
    public ulong clientId;
    public string playerName;
    public string playerTurn;
    private NetworkHandler networkHandler;

    protected void Start()
    {
        EventSystem.Instance.AddSubscriber( this );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkHandler = NetworkManager.Singleton.GetComponent<NetworkHandler>();

        if( IsServer )
        {
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectedCallback;
        }
        else if( IsClient )
        {
            OnNetworkSpawnServerRpc( networkHandler.localPlayerData.name );
        }
    }

    [ServerRpc( RequireOwnership = false )]
    public void OnNetworkSpawnServerRpc( string player, ServerRpcParams serverRpcParams = default )
    {
        networkHandler.playerIdsByName[player] = serverRpcParams.Receive.SenderClientId;
    }

    private void NetworkManager_OnClientConnectedCallback( ulong clientId )
    {
        this.clientId = clientId;
    }

    private void NetworkManager_OnClientDisconnectedCallback( ulong clientId )
    {
        Debug.Log( "Player disconnected: " + playerName );
        if( clientId == this.clientId )
            ExitGameClientRpc();
    }

    [ServerRpc( RequireOwnership = false )]
    private void RequestExitGameServerRpc()
    {
        ExitGameClientRpc();
    }

    [ClientRpc]
    private void ExitGameClientRpc()
    {
        EventSystem.Instance.TriggerEvent( new PlayerDisconnectedEvent()
        {
            player = playerName
        } );
    }

    public void OnEventReceived( IBaseEvent e )
    {
        if( e is TurnStartEvent turnStart )
        {
            playerTurn = turnStart.player;
            isPlayerTurn = turnStart.player == networkHandler.localPlayerData.name;
        }
        else if( e is StartGameEvent startgame )
        {
            var found = startgame.playerData.Find( x => x.clientId == this.clientId );
            playerName = found.name;
        }
        else if( e is ExitGameEvent exitGame )
        {
            // Player has chosen to leave game via menu
            if( !exitGame.fromGameOver )
            {
                RequestExitGameServerRpc();
            }
        }
    }
}