using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private DateTime lastLobbyUpdate;

    const string RngSeedKey = "RngSeed";
    const string PlayerNameKey = "PlayerName";
    const string StartGameKey = "StartGame";
    const string RelayConnectionType = "dtls";

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

        // ParrelSync should only be used within the Unity Editor so you should use the UNITY_EDITOR define
#if UNITY_EDITOR
        if( ParrelSync.ClonesManager.IsClone() )
        {
            // When using a ParrelSync clone, switch to a different authentication profile to force the clone
            // to sign in as a different anonymous user account.
            string customArgument = ParrelSync.ClonesManager.GetArgument();
            AuthenticationService.Instance.SwitchProfile( $"Clone_{customArgument}_Profile" );
        }
#endif
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
                { PlayerNameKey, new PlayerDataObject( PlayerDataObject.VisibilityOptions.Member, localPlayerName ) }
            }
        };
    }

    private Dictionary<string, DataObject> CreateLobbyData( string gameStartJoinCode )
    {
        var data = new Dictionary<string, DataObject>();
        data.Add( RngSeedKey, new DataObject( DataObject.VisibilityOptions.Member, GameConstants.Instance.rngSeed.ToString() ) );
        if( gameStartJoinCode != null )
            data.Add( StartGameKey, new DataObject( DataObject.VisibilityOptions.Member, gameStartJoinCode ) );
        return data;
    }

    public async void HostLobby()
    {
        try
        {
            var lobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = true,
                Player = CreateNetworkPlayerData(),
                Data = CreateLobbyData( null )
            };

            lobby = await LobbyService.Instance.CreateLobbyAsync( localPlayerName, 4, lobbyOptions );
            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent() 
            { 
                lobby = lobby,
                playerNames = GetPlayerNames()
            } );
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

        lobby = null;
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
            GameConstants.Instance.rngSeedRuntime = int.Parse( lobby.Data[RngSeedKey].Value );
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

                EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent() 
                { 
                    lobby = lobby,
                    playerNames = GetPlayerNames()
                } );

                if( lobby.Data != null && lobby.Data.TryGetValue( StartGameKey, out var joinCode ) )
                {
                    if( lobbyHeartbeatCoroutine != null )
                        StopCoroutine( lobbyHeartbeatCoroutine );
                    if( lobby.HostId != AuthenticationService.Instance.PlayerId )
                        StartGameClient( joinCode.Value );
                    lobby = null;
                    break;
                }
            }
        }
    }

    private List<string> GetPlayerNames()
    {
        return lobby.Players.Select( x => x.Data[PlayerNameKey].Value ).ToList();
    }

    public async void StartGame()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync( 1 );
            var joinCode = await RelayService.Instance.GetJoinCodeAsync( allocation.AllocationId );
            var serverData = new RelayServerData( allocation, RelayConnectionType );
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
            NetworkManager.Singleton.StartHost();

            lobby = await Lobbies.Instance.UpdateLobbyAsync( lobby.Id, new UpdateLobbyOptions()
            {
                Data = CreateLobbyData( joinCode )
            } );

            if( lobbyHeartbeatCoroutine != null )
                StopCoroutine( lobbyHeartbeatCoroutine );

            if( lobbyPollCoroutine != null )
                StopCoroutine( lobbyPollCoroutine );

            EventSystem.Instance.TriggerEvent( new PreStartGameEvent() );
            EventSystem.Instance.TriggerEvent( new StartGameEvent() { playerNames = GetPlayerNames() } );
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
            var playerNames = GetPlayerNames(); // Must be done before leaving lobby
            LeaveLobby();
            var allocation = await RelayService.Instance.JoinAllocationAsync( relayJoinCode );
            var serverData = new RelayServerData( allocation, RelayConnectionType );
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
            NetworkManager.Singleton.StartClient();

            EventSystem.Instance.TriggerEvent( new PreStartGameEvent() );
            EventSystem.Instance.TriggerEvent( new StartGameEvent() { playerNames = playerNames } );
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
