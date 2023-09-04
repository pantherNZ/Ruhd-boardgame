using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour, IEventReceiver
{
    private NetworkHandler networkHandler;
    public bool isPlayerTurn = false;

    protected void Start()
    {
        EventSystem.Instance.AddSubscriber( this );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkHandler = NetworkManager.Singleton.GetComponent<NetworkHandler>();
    }

    public void OnEventReceived( IBaseEvent e )
    {
        if( e is TurnStartEvent turnStart )
        {
            isPlayerTurn = turnStart.player == networkHandler.localPlayerData.name;
        }
    }
}