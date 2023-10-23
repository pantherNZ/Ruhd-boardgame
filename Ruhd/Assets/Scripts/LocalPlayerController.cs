using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

public class LocalPlayerController : EventReceiverInstance
{
    public override void OnEventReceived( IBaseEvent e )
    {
       
    }

    private void Update()
    {
        if( Input.GetKeyDown( KeyCode.Escape ) )
            EventSystem.Instance.TriggerEvent( new RequestPauseGameEvent() );
    }
}