using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    private NetworkHandler networkHandler;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkHandler = NetworkManager.Singleton.GetComponent<NetworkHandler>();
    }
}