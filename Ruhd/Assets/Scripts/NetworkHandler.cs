using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class NetworkHandler : MonoBehaviour
{
    private string playerName;
    private Lobby hostLobby;
    private Lobby joinedLobby;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log( "Signed in: " + AuthenticationService.Instance.PlayerId );
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void ConfirmName( TMPro.TMP_InputField input )
    {
        playerName = input.text;
    }

    public async void HostGame()
    {
        try
        {
            hostLobby = await LobbyService.Instance.CreateLobbyAsync( playerName, 2 );
            Debug.Log( "Lobby created: " + hostLobby.Name + $" ({hostLobby.LobbyCode})" );

            StartCoroutine( SendHostHeartBeat() );
            NetworkManager.Singleton.StartServer();
        } 
        catch(LobbyServiceException e)
        {
            Debug.LogError( e );
        }
    }

    private IEnumerator SendHostHeartBeat()
    {
        while( hostLobby != null )
        {
            yield return new WaitForSeconds( 20.0f );
            var sendHeartBeat = LobbyService.Instance.SendHeartbeatPingAsync( hostLobby.Id );
            Task.Run( () => sendHeartBeat );
        }
    }

    public async void JoinGame( TMPro.TMP_InputField code )
    {
        try
        {
            await Lobbies.Instance.JoinLobbyByCodeAsync( code.name );

            NetworkManager.Singleton.StartClient();
        }
        catch( LobbyServiceException e )
        {
            Debug.LogError( e );
        }
    }

    public async void CloseOnlineGame()
    {
        try
        {
            if( hostLobby != null )
            {
                await Lobbies.Instance.DeleteLobbyAsync( hostLobby.Id );
            }
            else
            {
                await Lobbies.Instance.RemovePlayerAsync( joinedLobby.Id, AuthenticationService.Instance.PlayerId );
            }
        }
       catch( LobbyServiceException e )
        {
            Debug.LogError( e );
        }
    }
}
