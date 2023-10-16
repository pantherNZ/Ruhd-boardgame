using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

public class BasePlayerController : IEventReceiver
{
    public bool isPlayerTurn = false;
    public ulong clientId;
    public string playerName;
    public string playerTurn;

    public void OnEventReceived( IBaseEvent e )
    {
       
    }
}