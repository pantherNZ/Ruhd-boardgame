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

    public void OnEventReceived( IBaseEvent e )
    {
        if( e is TurnStartEvent turnStart )
        {
            isPlayerTurn = turnStart.player == networkHandler.localPlayerData.name;
        }
        else if( e is StartGameEvent startgame )
        {
            var found = startgame.playerData.Find( x => x.clientId == this.clientId );
            playerName = found.name;
        }
        else if( e is ChallengeStartedEvent challengeStarted )
        {
            //challengeStarted.challengeData.tile
        }
    }
}