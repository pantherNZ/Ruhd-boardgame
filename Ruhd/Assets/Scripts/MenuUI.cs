using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

enum MenuState
{
    Title,
    Buttons,
    HostGame,
    JoinGame,
    Lobby,
    InGameMenu,
}

public class MenuUI : EventReceiverInstance
{
    [SerializeField] Image background;
    [SerializeField] Image blur;
    [SerializeField] CanvasGroup titleArea;
    [SerializeField] DeckHandler deck;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 padding;
    [SerializeField] int flippedChancePercent;
    [SerializeField] int howToPlayChancePercent;
    [SerializeField] GameObject playTilePrefab;
    [SerializeField] GameObject howToPlayTilePrefab;
    [SerializeField] RectTransform centreMenuArea;
    [SerializeField] CanvasGroup titleScreen;
    [SerializeField] CanvasGroup buttonsScreen;
    [SerializeField] CanvasGroup hostGameInputScreen;
    [SerializeField] CanvasGroup joinGameInputScreen;
    [SerializeField] CanvasGroup lobbyGameScreen;
    [SerializeField] CanvasGroup inGameMenuScreen;

    [Header( "Animation" )]
    [SerializeField] float fadeTimeSec = 0.5f;
    [SerializeField] float tileMoveTimerMin = 2.0f;
    [SerializeField] float tileMoveTimerMax = 10.0f;
    [SerializeField] Utility.EasingFunctionTypes easingFunction;
    [SerializeField] Utility.EasingFunctionMethod easingMethod;
    [SerializeField] float easingSpeedMove = 1.0f;
    [SerializeField] float easingSpeedRotate = 1.0f;
    [SerializeField] float menuFadeOutTimeSec = 1.0f;
    [SerializeField] float menuFadeOutDelaySec = 1.0f;
    [SerializeField] float menuFadeOutDelayPerTileSec = 0.002f;
    [SerializeField] Utility.ShakeParams shake;

    private List<GameObject> grid = new List<GameObject>();
    private List<GameObject> validTiles = new List<GameObject>();
    private MenuState state = MenuState.Title;

    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;

    private Vector2Int gridSize;
    private bool centreHighlight;

    private List<CanvasGroup> stateScreens;

    private Coroutine rotateTilesRoutine;
    private Coroutine swapTilesRoutine;
    private Coroutine swapTilesRoutine1;
    private Coroutine swapTilesRoutine2;
    private List<Transform> movingTiles = new List<Transform>();

    private bool interactable = true;

    protected override void Start()
    {
        base.Start();

        base.modifyListenerWithEnableDisable = false;

        stateScreens = new List<CanvasGroup>()
        {
            titleScreen,
            buttonsScreen,
            hostGameInputScreen,
            joinGameInputScreen,
            lobbyGameScreen,
            inGameMenuScreen,
        };

        Init();
    }

    private void Init()
    {
        StopAnimations();
        interactable = true;
        state = MenuState.Title;
        gameObject.SetActive( true );

        foreach( var tile in grid )
            if( tile != null )
                tile.Destroy();

        grid.Clear();
        validTiles.Clear();

        foreach( var screen in stateScreens )
        {
            screen.SetVisibility( false );
            screen.gameObject.SetActive( false );
        }

        titleScreen.SetVisibility( true );
        titleScreen.gameObject.SetActive( true );

        var cameraRect = Camera.main.pixelRect;
        cameraRect.size -= cellSize * 2.0f; 
        cameraRect.center = new Vector2( 0, 0 );

        var expandedRect = cameraRect;
        expandedRect.size += cellSize * 2.0f;
        expandedRect.center = new Vector2( 0, 0 );

        gridSize = new Vector2Int(
            Mathf.RoundToInt( expandedRect.width / cellSize.x ),
            Mathf.RoundToInt( expandedRect.height / cellSize.y ) );
        gridSize.x += ( gridSize.x & 1 ) == 1 ? 1 : 0;
        gridSize.y += ( gridSize.y & 1 ) == 1 ? 1 : 0;

        for( int y = -gridSize.y / 2; y < gridSize.y / 2; ++y )
        {
            for( int x = -gridSize.x / 2; x < gridSize.x / 2; ++x )
            {
                if( y >= -1 && y < 1 && x >= -2 && x < 2 )
                {
                    grid.Add( null );
                    continue;
                }

                if( deck.IsDeckEmpty() )
                    deck.Reset( 10, Utility.DefaultRng );

                var newPosition = GetPosition( new Vector2Int( x, y ) );
                var tile = deck.DrawTile( Utility.DefaultRng );
                tile.draggable = false;
                grid.Add( tile.gameObject );
                tile.transform.SetParent( background.transform, false );
                var rectTransform = tile.transform as RectTransform;
                rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
                rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
                rectTransform.anchoredPosition = newPosition;

                tile.GetComponent<EventDispatcherV2>().OnPointerUpEvent.AddListener( x =>
                {
                    if( gameObject.activeSelf )
                    {
                        switch( Random.Range( 0, 3 ) )
                        {
                            case 0: RotateTileRandomly( tile.gameObject ); break;
                            case 1: SwapTileRandomly( tile.gameObject ); break;
                            case 2: StartCoroutine( Shake( tile.transform ) ); break;
                        }
                    }
                } );

                if( cameraRect.Contains( newPosition ) && !( y >= -2 && y < 2 && x >= -3 && x < 3 ) )
                    validTiles.Add( tile.gameObject );

                //if( y >= -2 && y < 2 && x >= -3 && x < 3 )
                //    continue;
                //
                //if( isOutsideCamera )
                //    continue;
                //
                //if( Random.Range( 0, 100 ) < flippedChancePercent )
                //    ReplaceTile( tile.gameObject, playTilePrefab, true );
                //else if( Random.Range( 0, 100 ) < howToPlayChancePercent )
                //    ReplaceTile( tile.gameObject, howToPlayTilePrefab, true );
            }
        }

        ReplaceTile( validTiles.RandomItem(), howToPlayTilePrefab, true );
        ReplaceTile( validTiles.RandomItem(), playTilePrefab, true );

        swapTilesRoutine = StartCoroutine( SwapTilesRandomly() );
        rotateTilesRoutine = StartCoroutine( RotateTilesRandomly() );
    }

    private int GetRandomNeighbour( int idx )
    {
        List<int> options = new List<int>();
        if( idx > 0 && grid[idx - 1] != null )
            options.Add( idx - 1 );
        if( idx < grid.Count - 1 && grid[idx + 1] != null )
            options.Add( idx + 1 );
        if( idx > gridSize.x && grid[idx - gridSize.x] != null )
            options.Add( idx - gridSize.x );
        if( idx < grid.Count - 1 - gridSize.x && grid[idx + gridSize.x] != null )
            options.Add( idx + gridSize.x );
        return options.RandomItem();
    }

    public IEnumerator Shake( Transform transform )
    {
        if( !movingTiles.Contains( transform ) )
        {
            movingTiles.Add( transform );
            yield return Utility.Shake( transform, shake );
            movingTiles.Remove( transform );
        }
    }

    private IEnumerator SwapTilesRandomly()
    {
        while( true )
        {
            var randomTileIdx = Random.Range( 0, grid.Count );
            var tile = grid[randomTileIdx];
            if( tile == null )
                continue;
            SwapTileRandomly( tile );
            yield return new WaitForSeconds( Random.Range( tileMoveTimerMin, tileMoveTimerMax ) );
        }
    }

    public void SwapTileRandomly( GameObject tile )
    {
        if( !movingTiles.Contains( tile.transform ) )
        {
            var tileIdx = grid.FindIndex( x => x == tile );
            var otherTileIdx = GetRandomNeighbour( tileIdx );
            var other = grid[otherTileIdx];
            if( !movingTiles.Contains( other.transform ) )
            {
                swapTilesRoutine1 = StartCoroutine( InterpolatePosition( tile.transform, other.transform.localPosition, easingSpeedMove, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) ) );
                swapTilesRoutine2 = StartCoroutine( InterpolatePosition( other.transform, tile.transform.localPosition, easingSpeedMove, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) ) );
                grid[tileIdx] = other;
                grid[otherTileIdx] = tile;
            }
        }
    }

    private IEnumerator InterpolatePosition( Transform transform, Vector3 targetPosition, float durationSec, bool localPosition, Utility.EasingFunction easingFunction )
    {
        if( !movingTiles.Contains( transform ) )
        {
            movingTiles.Add( transform );
            yield return Utility.InterpolatePosition( transform, targetPosition, durationSec, localPosition, easingFunction );
            movingTiles.Remove( transform );
        }
    }

    private IEnumerator RotateTilesRandomly()
    {
        while( true )
        {
            yield return new WaitForSeconds( Random.Range( tileMoveTimerMin, tileMoveTimerMax ) );
            RotateTileRandomly( validTiles.RandomItem() );
        }
    }

    public void RotateTileRandomly( GameObject tile )
    {
        var rotation = new Vector3( 0.0f, 0.0f, 90.0f * ( Utility.DefaultRng.Bool() ? 1 : -1 ) * ( Utility.DefaultRng.Bool() ? 2 : 1 ) );
        StartCoroutine( InterpolateRotation( tile.transform, rotation, easingSpeedRotate, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) ) );
    }

    private IEnumerator InterpolateRotation( Transform transform, Vector3 rotation, float durationSec, bool localRotation, Utility.EasingFunction easingFunction)
    {
        if( !movingTiles.Contains( transform ) )
        {
            movingTiles.Add( transform );
            yield return Utility.InterpolateRotation( transform, rotation, durationSec, localRotation, easingFunction );
            movingTiles.Remove( transform );
        }
    }

    private void ReplaceTile( GameObject replacee, GameObject prefab, bool resetRotation )
    {
        var replacement = Instantiate( prefab, transform );
        ( replacement.transform as RectTransform ).anchorMin = new Vector2( 0.5f, 0.5f );
        ( replacement.transform as RectTransform ).anchorMax = new Vector2( 0.5f, 0.5f );
        replacement.transform.Match( replacee.transform );
        if( resetRotation )
            replacement.transform.rotation = Quaternion.identity;
        replacee.Destroy();
        validTiles[validTiles.FindIndex( x => x == replacee )] = replacement;
        grid[grid.FindIndex( x => x == replacee )] = replacement;
    }

    private Vector2 GetPosition( Vector2Int pos )
    {
        return cellSize / 2.0f + padding / 2.0f + new Vector2(
            pos.x * ( cellSize.x + padding.x ),
            pos.y * ( cellSize.y + padding.y ) );
    }

    private void Update()
    {
        if( !interactable )
            return;

        if( state == MenuState.Title || state == MenuState.Buttons )
        {
            if( centreMenuArea != null && centreHighlight != centreMenuArea.GetSceenSpaceRect().Contains( Utility.GetMouseOrTouchPos() ) )
            {
                centreHighlight = !centreHighlight;
                ToggleFadeText( 
                    centreHighlight ? titleScreen : buttonsScreen,
                    centreHighlight ? buttonsScreen : titleScreen );
                state = centreHighlight ? MenuState.Buttons : MenuState.Title;
            }
        }
    }

    public void ChangeStateByName( string stateName )
    {
        ChangeStateByName( stateName, false );
    }

    public void ChangeStateByName( string stateName, bool instant )
    {
        if( !interactable )
            return;

        var newState = Utility.ParseEnum<MenuState>( stateName );
        ToggleFadeText( stateScreens[( int )state], stateScreens[( int )newState], instant );
        state = newState;
    }

    public void RequestStartGame()
    {
        if( !interactable )
            return;

        RequestStartGame( null );
    }

    public void ShowInGameMenu( bool show )
    {
        if( show )
            Init();
        if( show )
            ChangeStateByName( "InGameMenu", true );
        ToggleMenu( show, 0.5f );
    }

    private void RequestStartGame( List<NetworkHandler.PlayerData> playerData)
    {
        if( !interactable )
            return;

        ToggleMenu( false );
        bool vsComputer = playerData == null;

        if( vsComputer )
        {
            var localPlayerName = "PLAYER";
            NetworkManager.Singleton.GetComponent<NetworkHandler>().localPlayerData.name = localPlayerName;

            playerData = new List<NetworkHandler.PlayerData>()
            {
                new NetworkHandler.PlayerData() { name = localPlayerName, type = NetworkHandler.PlayerType.Local },
                new NetworkHandler.PlayerData() { name = "AI", type = NetworkHandler.PlayerType.AI },
            };
        }

        EventSystem.Instance.TriggerEvent( new PreStartGameEvent() );
        EventSystem.Instance.TriggerEvent( new StartGameEvent() 
        { 
            playerData = playerData, 
            vsComputer = vsComputer 
        } );
    }

    public void LeaveGame()
    {
        EventSystem.Instance.TriggerEvent( new ExitGameEvent() );
    }

    private void StopAnimations()
    {
        this.TryStopCoroutine( swapTilesRoutine );
        this.TryStopCoroutine( rotateTilesRoutine );
        this.TryStopCoroutine( swapTilesRoutine1 );
        this.TryStopCoroutine( swapTilesRoutine2 );
    }

    private void ToggleMenu( bool show, float timeMultiplier = 1.0f )
    {
        gameObject.SetActive( true );
        StartCoroutine( ToggleMenuCoroutine( show, timeMultiplier ) );
    }

    private IEnumerator ToggleMenuCoroutine( bool show, float timeMultiplier = 1.0f )
    {
        StopAnimations();
        interactable = false;

        foreach( var tile in grid )
        {
            if( tile == null )
                continue;
            tile.GetComponent<EventDispatcherV2>().enabled = false;
            var expandedRect = Camera.main.pixelRect;
            expandedRect.size += cellSize * 2.0f;
            var originPos = tile.transform.localPosition.ToVector2();
            var signPos = new Vector2( Mathf.Sign( originPos.x ), Mathf.Sign( originPos.y ) );
            var boundary = new Vector2( signPos.x * expandedRect.width / 2.0f, signPos.y * expandedRect.height / 2.0f );
            var closest = new Vector2( Mathf.Abs( boundary.x - originPos.x ), Mathf.Abs( boundary.y - originPos.y ) );
            var exitPosition = new Vector2( closest.x < closest.y ? boundary.x : originPos.x, closest.x < closest.y ? originPos.y : boundary.y );
            tile.transform.localPosition = show ? exitPosition : originPos;

            var baseDelay = menuFadeOutDelaySec + Mathf.Round( ( ( exitPosition - originPos ).magnitude - cellSize.x * 2.0f ) / cellSize.x );
            var tileFadeDelay = ( baseDelay * menuFadeOutDelayPerTileSec + Random.Range( -0.1f, 0.1f ) ) * timeMultiplier;

            Utility.FunctionTimer.CreateTimer( tileFadeDelay, () =>
            {
                var tileFadeTime = ( menuFadeOutTimeSec + Random.Range( -0.1f, 0.1f ) ) * timeMultiplier;
                var easingFunc = show ? new Utility.EasingFunction( Utility.Easing.Quintic.Out ) : new Utility.EasingFunction( Utility.Easing.Quintic.In );
                StartCoroutine( Utility.InterpolatePosition( tile.transform, show ? originPos : exitPosition, tileFadeTime, true, easingFunc ) );
            } );
        }

        if( show )
        {
            this.FadeToColour( background, Color.black, 1.0f * timeMultiplier, Utility.Easing.Quintic.In );
            this.FadeToColour( blur, Color.white, 0.5f * timeMultiplier );
            this.FadeFromTransparent( titleArea, 0.5f * timeMultiplier );
            yield return new WaitForSeconds( 2.0f * timeMultiplier );
            interactable = true;
        }
        else
        {
            this.FadeToColour( background, Color.clear, 0.5f * timeMultiplier );
            this.FadeToColour( blur, Color.clear, 1.0f * timeMultiplier );
            this.FadeToTransparent( titleArea, 0.5f * timeMultiplier );
            yield return new WaitForSeconds( 2.0f * timeMultiplier );
            gameObject.SetActive( false );
        }
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is RequestStartGameEvent requestStartGameEvent )
        {
            RequestStartGame( requestStartGameEvent.playerData );
        }
        else if( e is ExitGameEvent exitGameEvent )
        {
            Init();
            gameObject.SetActive( false );
            if( exitGameEvent.fromGameOver )
                ToggleMenu( true );
            NetworkManager.Singleton.GetComponent<NetworkHandler>().ExitGame();
        }
        else if( e is RequestTogglePauseGameEvent )
        {
            if( !gameObject.activeSelf || interactable )
                ShowInGameMenu( !gameObject.activeSelf );
        }
    }

    public void ToggleFadeText( CanvasGroup fadeOut, CanvasGroup fadeIn, bool instant = false )
    {
        fadeIn.gameObject.SetActive( true );
        fadeOut.gameObject.SetActive( !instant );

        this.TryStopCoroutine( fadeInCoroutine );
        this.TryStopCoroutine( fadeOutCoroutine );

        if( instant )
        {
            fadeIn.SetVisibility( true );
            fadeOut.SetVisibility( false );
        }
        else
        {
            fadeInCoroutine = StartCoroutine( Utility.FadeFromTransparent( fadeIn, fadeTimeSec ) );
            fadeOutCoroutine = StartCoroutine( Utility.FadeToTransparent( fadeOut, fadeTimeSec, null, true ) );
        }
    }
}
