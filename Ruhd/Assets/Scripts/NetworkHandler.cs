using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class NetworkHandler : MonoBehaviour
{
    public string localPlayerName;
    private Lobby lobby;
    private Coroutine lobbyHeartbeatCoroutine;
    private Coroutine lobbyPollCoroutine;
    private System.DateTime lastLobbyUpdate;

    async void Start()
    {
        var guid = Guid.NewGuid().ToString( "n" );
        guid = guid[..Mathf.Min( guid.Length, 30 )];
        var options = new InitializationOptions();
        options.SetProfile( guid );
        await UnityServices.InitializeAsync( options );

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log( "Signed in: " + AuthenticationService.Instance.PlayerId );
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void ConfirmName( TMPro.TMP_InputField input )
    {
        localPlayerName = input.text;
    }

    private Player CreateNetworkPlayerData()
    {
        return new Player()
        {
            Data = new Dictionary<string, PlayerDataObject>()
            {
                { "PlayerName", new PlayerDataObject( PlayerDataObject.VisibilityOptions.Member, localPlayerName ) }
            }
        };
    }

    public async void HostLobby()
    {
        try
        {
            var lobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = true,
                Player = CreateNetworkPlayerData(),
            };
            lobby = await LobbyService.Instance.CreateLobbyAsync( localPlayerName, 4, lobbyOptions );
            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent() { lobby = lobby } );
            lobbyHeartbeatCoroutine = StartCoroutine( SendLobbyHeartbeat() );
            lobbyPollCoroutine = StartCoroutine( PollLobbyUpdates() );
        }
        catch( LobbyServiceException e )
        {
            Debug.LogError( e );
        }
    }

    public async void LeaveLobby()
    {
        if( lobbyHeartbeatCoroutine != null )
            StopCoroutine( lobbyHeartbeatCoroutine );

        if( lobbyPollCoroutine != null )
            StopCoroutine( lobbyPollCoroutine );

        if( lobby != null )
            await LobbyService.Instance.RemovePlayerAsync( lobby.Id, AuthenticationService.Instance.PlayerId );
    }

    public async void JoinLobby( TMPro.TMP_InputField code )
    {
        try
        {
            var joinOptions = new JoinLobbyByCodeOptions()
            {
                Player = CreateNetworkPlayerData()
            };
            lobby = await Lobbies.Instance.JoinLobbyByCodeAsync( code.text, joinOptions );
            lobbyPollCoroutine = StartCoroutine( PollLobbyUpdates() );
        }
        catch( LobbyServiceException e )
        {
            Debug.LogError( e );
        }
    }

    private IEnumerator SendLobbyHeartbeat()
    {
        while( true )
        {
            yield return new WaitForSeconds( 20.0f );
            var sendHeartbeat = LobbyService.Instance.SendHeartbeatPingAsync( lobby.Id );
            yield return new WaitUntil( () => sendHeartbeat.IsCompleted );
        }
    }

    private IEnumerator PollLobbyUpdates()
    {
        while( true )
        {
            yield return new WaitForSeconds( 2.0f );
            var getLobbytask = LobbyService.Instance.GetLobbyAsync( lobby.Id );
            yield return new WaitUntil( () => getLobbytask.IsCompleted );
            lobby = getLobbytask.Result;

            if( lobby.LastUpdated > lastLobbyUpdate )
            {
                lastLobbyUpdate = lobby.LastUpdated;

                EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent() { lobby = lobby } );

                if( lobby.Data != null && lobby.Data.TryGetValue( "StartGame", out var joinCode ) )
                {
                    StopCoroutine( lobbyHeartbeatCoroutine );
                    var isHost = lobby.HostId == AuthenticationService.Instance.PlayerId;
                    lobby = null;
                    if( !isHost )
                        StartGameClient( joinCode.Value );
                    break;
                }
            }
        }
    }

    public async void StartGame()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync( 1 );
            var joinCode = await RelayService.Instance.GetJoinCodeAsync( allocation.AllocationId );
            var serverData = new RelayServerData( allocation, "dtls" );
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
            NetworkManager.Singleton.StartHost();

            lobby = await Lobbies.Instance.UpdateLobbyAsync( lobby.Id, new UpdateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>()
                {
                    { "StartGame", new DataObject( DataObject.VisibilityOptions.Member, joinCode ) }
                }
            } );

            if( lobbyHeartbeatCoroutine != null )
                StopCoroutine( lobbyHeartbeatCoroutine );

            if( lobbyPollCoroutine != null )
                StopCoroutine( lobbyPollCoroutine );
        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
        }
    }

    private async void StartGameClient( string relayJoinCode )
    {
        try
        {
            LeaveLobby();
            var allocation = await RelayService.Instance.JoinAllocationAsync( relayJoinCode );
            var serverData = new RelayServerData( allocation, "dtls" );
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
            NetworkManager.Singleton.StartClient();
        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
        }
    }

    public void CloseOnlineGame()
    {
        try
        {

        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
        }
    }
}
