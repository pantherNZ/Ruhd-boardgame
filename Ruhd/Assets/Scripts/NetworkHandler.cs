using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public enum PlayerType
    {
        Local,
        AI,
        Network,
    }

    public struct PlayerData
    {
        public string id;
        public string name;
        public bool isReady;
        public ulong clientId;
        public PlayerType type;
        public bool isRemotePlayer;
    }

    public PlayerData localPlayerData;
    public RateLimiter lobbyRateLimiter = new RateLimiter( 2, TimeSpan.FromSeconds( 6.0f ) );
    public readonly Dictionary<string, ulong> playerIdsByName = new Dictionary<string, ulong>();

    private Lobby lobby;
    private ILobbyEvents lobbyEventsListener;
    private Coroutine lobbyHeartbeatCoroutine;

    const string RngSeedKey = "RngSeed";
    const string PlayerNameKey = "PlayerName";
    const string PlayerReadyKey = "PlayerReady";
    const string StartGameKey = "StartGame";
    const string RelayJoinCodeKey = "RelayCode";
    const string RelayConnectionType = "dtls";

    Unity.Services.Relay.Models.Allocation allocation;

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
//#if UNITY_EDITOR
//        if( ParrelSync.ClonesManager.IsClone() )
//        {
//            // When using a ParrelSync clone, switch to a different authentication profile to force the clone
//            // to sign in as a different anonymous user account.
//            string customArgument = ParrelSync.ClonesManager.GetArgument();
//            AuthenticationService.Instance.SwitchProfile( $"Clone_{customArgument}_Profile" );
//        }
//#endif
    }

    public void ConfirmName( TMPro.TMP_InputField input )
    {
        localPlayerData.name = input.text;
    }

    private Player CreateNetworkPlayer()
    {
        return new Player()
        {
            Data = CreateNetworkPlayerData()
        };
    }

    private Dictionary<string, PlayerDataObject> CreateNetworkPlayerData()
    {
        var data = new Dictionary<string, PlayerDataObject>
        {
            { PlayerNameKey, new PlayerDataObject( PlayerDataObject.VisibilityOptions.Member, localPlayerData.name ) },
            { PlayerReadyKey, new PlayerDataObject( PlayerDataObject.VisibilityOptions.Member, localPlayerData.isReady ? "READY" : null ) }
        };
        return data;
    }

    private Dictionary<string, DataObject> CreateLobbyData( string gameStartJoinCode, bool startGame = false )
    {
        var data = new Dictionary<string, DataObject>
        {
            { RngSeedKey, new DataObject( DataObject.VisibilityOptions.Member, GameController.Instance.rngSeedRuntime.ToString() ) }
        };
        if( gameStartJoinCode != null )
            data.Add( RelayJoinCodeKey, new DataObject( DataObject.VisibilityOptions.Member, gameStartJoinCode ) );
        if( startGame )
            data.Add( StartGameKey, new DataObject( DataObject.VisibilityOptions.Member, "STARTGAME" ) );
        return data;
    }

    public async Task<RequestFailedException> HostLobby( string name )
    {
        localPlayerData.name = name;

        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync( 1 );
            var serverData = new RelayServerData( allocation, RelayConnectionType );
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
            var joinCode = await RelayService.Instance.GetJoinCodeAsync( allocation.AllocationId );
            GameController.Instance.InitRng();

            var lobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = true,
                Player = CreateNetworkPlayer(),
                Data = CreateLobbyData( joinCode )
            };

            lobby = await LobbyService.Instance.CreateLobbyAsync( localPlayerData.name, 4, lobbyOptions );
            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent() 
            { 
                lobby = lobby,
                playerData = GetPlayerData()
            } );
            lobbyHeartbeatCoroutine = StartCoroutine( SendLobbyHeartbeat() );
            ListenForLobbyUpdates();
            NetworkManager.Singleton.StartHost();
            playerIdsByName.Clear();
            playerIdsByName[name] = NetworkManager.Singleton.LocalClientId;
            return null;
        }
        catch( LobbyServiceException e )
        {
            Debug.LogError( e );
            return e;
        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
            return e;
        }
    }

    public async void LeaveLobby()
    {
        if( lobbyHeartbeatCoroutine != null )
            StopCoroutine( lobbyHeartbeatCoroutine );

        if( lobbyEventsListener != null )
            await lobbyEventsListener.UnsubscribeAsync();

        if( lobby != null )
        {
            try
            {
                if( AuthenticationService.Instance.PlayerId == lobby.Id )
                    await LobbyService.Instance.DeleteLobbyAsync( lobby.Id );
                else
                    await LobbyService.Instance.RemovePlayerAsync( lobby.Id, AuthenticationService.Instance.PlayerId );
            }
            catch( LobbyServiceException )
            {
                // This is valid to fail, as server will close lobby forcefully when game starts
            }
        }

        lobby = null;
    }

    public async void ToggleReadyForGame()
    {
        localPlayerData.isReady = !localPlayerData.isReady;

        try
        {
            var updateOptions = new UpdatePlayerOptions()
            {
                Data = CreateNetworkPlayerData()
            };

            lobby = await Lobbies.Instance.UpdatePlayerAsync( lobby.Id, AuthenticationService.Instance.PlayerId, updateOptions );

            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent()
            {
                lobby = lobby,
                playerData = GetPlayerData(),
            } );
        }
        catch( LobbyServiceException e )
        {
            if( e.Reason != LobbyExceptionReason.LobbyNotFound )
                Debug.LogError( e );
        }
    }

    public async Task<RequestFailedException> JoinLobby( string code, string name )
    {
        localPlayerData.name = name;

        try
        {
            var joinOptions = new JoinLobbyByCodeOptions()
            {
                Player = CreateNetworkPlayer()
            };

            lobby = await Lobbies.Instance.JoinLobbyByCodeAsync( code, joinOptions );
            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent()
            {
                lobby = lobby,
                playerData = GetPlayerData(),
            } );
            ListenForLobbyUpdates();
            GameController.Instance.InitRng( int.Parse( lobby.Data[RngSeedKey].Value ) );

            if( lobby.Data.TryGetValue( RelayJoinCodeKey, out var joinCode ) )
            {
                if( lobby.HostId != AuthenticationService.Instance.PlayerId )
                {
                    var allocation = await RelayService.Instance.JoinAllocationAsync( joinCode.Value );
                    var serverData = new RelayServerData( allocation, RelayConnectionType );
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData( serverData );
                    NetworkManager.Singleton.StartClient();
                }
            }

            return null;
        }
        catch( LobbyServiceException e )
        {
            if( e.Reason != LobbyExceptionReason.LobbyNotFound )
                Debug.LogError( e );
            return e;
        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
            return e;
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

    private async void ListenForLobbyUpdates()
    {
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.KickedFromLobby += OnKickedFromLobby;
        callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
        try
        {
            lobbyEventsListener = await Lobbies.Instance.SubscribeToLobbyEventsAsync( lobby.Id, callbacks );
        }
        catch( LobbyServiceException ex )
        {
            switch( ex.Reason )
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning( $"Already subscribed to lobby[{lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}" ); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError( $"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}" ); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError( $"Failed to connect to lobby events. Exception Message: {ex.Message}" ); throw;
                default: throw;
            }
        }
    }

    private void OnLobbyChanged( ILobbyChanges changes )
    {
        if( changes.LobbyDeleted )
        {
            LeaveLobby();
        }
        else
        {
            changes.ApplyToLobby( lobby );

            EventSystem.Instance.TriggerEvent( new LobbyUpdatedEvent()
            {
                lobby = lobby,
                playerData = GetPlayerData(),
            } );

            if( lobby.Data != null )
            {
                if( lobby.Data.ContainsKey( StartGameKey ) )
                {
                    if( lobby.HostId != AuthenticationService.Instance.PlayerId )
                        StartGameClient();
                    LeaveLobby();
                }
            }
        }
    }

    private void OnKickedFromLobby()
    {
        // These events will never trigger again, so let’s remove it.
        lobbyEventsListener = null;
        // Refresh the UI in some way
    }

    private void OnLobbyEventConnectionStateChanged( LobbyEventConnectionState state )
    {
        switch( state )
        {
            case LobbyEventConnectionState.Unsubscribed: /* Update the UI if necessary, as the subscription has been stopped. */ break;
            case LobbyEventConnectionState.Subscribing: /* Update the UI if necessary, while waiting to be subscribed. */ break;
            case LobbyEventConnectionState.Subscribed: /* Update the UI if necessary, to show subscription is working. */ break;
            case LobbyEventConnectionState.Unsynced: /* Update the UI to show connection problems. Lobby will attempt to reconnect automatically. */ break;
            case LobbyEventConnectionState.Error: /* Update the UI to show the connection has errored. Lobby will not attempt to reconnect as something has gone wrong. */
            default: break;
        }
    }

    private List<PlayerData> GetPlayerData()
    {
        return lobby.Players.Select( x =>
        {
            return new PlayerData()
            {
                id = x.Id,
                name = x.Data[PlayerNameKey].Value,
                isReady = x.Data[PlayerReadyKey].Value != null,
                clientId = playerIdsByName.GetValueOrDefault( x.Data[PlayerNameKey].Value ),
                type = NetworkHandler.PlayerType.Network,
                isRemotePlayer = x.Data[PlayerNameKey].Value != localPlayerData.name,
            };
        } ).ToList();
    }

    public async void StartGame()
    {
        try
        {
            var joinCode = await RelayService.Instance.GetJoinCodeAsync( allocation.AllocationId );

            lobby = await Lobbies.Instance.UpdateLobbyAsync( lobby.Id, new UpdateLobbyOptions()
            {
                Data = CreateLobbyData( joinCode, true )
            } );

            if( lobbyHeartbeatCoroutine != null )
                StopCoroutine( lobbyHeartbeatCoroutine );

            EventSystem.Instance.TriggerEvent( new RequestStartGameEvent(){ playerData = GetPlayerData() } );
            Utility.FunctionTimer.CreateTimer( 5.0f, LeaveLobby );
        }
        catch( RelayServiceException e )
        {
            Debug.LogError( e );
        }
    }

    private void StartGameClient()
    {
        try
        {
            var playerData = GetPlayerData(); // Must be done before leaving lobby
            LeaveLobby();
            EventSystem.Instance.TriggerEvent( new RequestStartGameEvent() { playerData = playerData } );
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
