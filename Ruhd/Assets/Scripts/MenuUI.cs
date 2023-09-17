using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum MenuState
{
    Title,
    Buttons,
    HostGame,
    JoinGame,
    Lobby,
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
            lobbyGameScreen
        };

        Init();
    }

    private void Init()
    {
        StopAnimations();

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
                    deck.Reset( 10 );

                var newPosition = GetPosition( new Vector2Int( x, y ) );
                var tile = deck.DrawTile( true );
                tile.draggable = false;
                grid.Add( tile.gameObject );
                var isOutsideCamera = !cameraRect.Contains( newPosition );
                if( !isOutsideCamera )
                    validTiles.Add( tile.gameObject );
                tile.transform.SetParent( background.transform, false );
                var rectTransform = tile.transform as RectTransform;
                rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
                rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
                rectTransform.anchoredPosition = newPosition;

                if( y >= -2 && y < 2 && x >= -3 && x < 3 )
                    continue;

                if( isOutsideCamera )
                    continue;

                if( Random.Range( 0, 100 ) < flippedChancePercent )
                    ReplaceTile( tile.gameObject, playTilePrefab, true );
                else if( Random.Range( 0, 100 ) < howToPlayChancePercent )
                    ReplaceTile( tile.gameObject, howToPlayTilePrefab, true );
            }
        }

        ReplaceTile( validTiles.RandomItem(), howToPlayTilePrefab, true );

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

    private IEnumerator SwapTilesRandomly()
    {
        while( true )
        {
            var randomTileIdx = Random.Range( 0, grid.Count );
            var tile = grid[randomTileIdx];
            if( tile == null )
                continue;
            yield return new WaitForSeconds( Random.Range( tileMoveTimerMin, tileMoveTimerMax ) );
            var otherTileIdx = GetRandomNeighbour( randomTileIdx );
            var other = grid[otherTileIdx];
            swapTilesRoutine1 = StartCoroutine( Utility.InterpolatePosition( tile.transform, other.transform.localPosition, easingSpeedMove, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) ) );
            swapTilesRoutine2 = StartCoroutine( Utility.InterpolatePosition( other.transform, tile.transform.localPosition, easingSpeedMove, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) ) );
            grid[randomTileIdx] = other;
            grid[otherTileIdx] = tile;
        }
    }

    private IEnumerator RotateTilesRandomly()
    {
        while( true )
        {
            yield return new WaitForSeconds( Random.Range( tileMoveTimerMin, tileMoveTimerMax ) );
            var tile = validTiles.RandomItem();
            var rotation = new Vector3( 0.0f, 0.0f, 90.0f * ( Utility.RandomBool() ? 1 : -1 ) * ( Utility.RandomBool() ? 2 : 1 ) );
            this.InterpolateRotation( tile.transform, rotation, easingSpeedRotate, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) );
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
        var newState = Utility.ParseEnum<MenuState>( stateName );
        ToggleFadeText( stateScreens[( int )state], stateScreens[( int )newState] );
        state = newState;
    }

    public void StartGame(bool vsComputer)
    {
        StartCoroutine( ToggleMenu( false ) );

        // If vsing PC, we need to manually call the start game event
        if( vsComputer )
        {
            var players = new List<NetworkHandler.PlayerData>()
            {
                new NetworkHandler.PlayerData() { name = "PLAYER" },
                new NetworkHandler.PlayerData() { name = "AI" },
            };

            EventSystem.Instance.TriggerEvent( new PreStartGameEvent() );
            EventSystem.Instance.TriggerEvent( new StartGameEvent() { playerData = players }, this );
        }
    }

    private void StopAnimations()
    {
        this.TryStopCoroutine( swapTilesRoutine );
        this.TryStopCoroutine( rotateTilesRoutine );
        this.TryStopCoroutine( swapTilesRoutine1 );
        this.TryStopCoroutine( swapTilesRoutine2 );
    }

    private IEnumerator ToggleMenu( bool show )
    {
        StopAnimations();
        gameObject.SetActive( true );

        foreach( var tile in grid )
        {
            if( tile == null )
                continue;
            var expandedRect = Camera.main.pixelRect;
            expandedRect.size += cellSize * 2.0f;
            var originPos = tile.transform.localPosition;
            var signPos = new Vector2( Mathf.Sign( originPos.x ), Mathf.Sign( originPos.y ) );
            var boundary = new Vector2( signPos.x * expandedRect.width / 2.0f, signPos.y * expandedRect.height / 2.0f );
            var closest = new Vector2( Mathf.Abs( boundary.x - originPos.x ), Mathf.Abs( boundary.y - originPos.y ) );
            var exitPosition = new Vector3( closest.x < closest.y ? boundary.x : originPos.x, closest.x < closest.y ? originPos.y: boundary.y );
            var delay = menuFadeOutDelaySec + Mathf.Round( ( ( exitPosition - originPos ).magnitude - cellSize.x * 2.0f ) / cellSize.x ) * menuFadeOutDelayPerTileSec;
            
            if( show )
                tile.transform.localPosition = exitPosition;

            Utility.FunctionTimer.CreateTimer( delay + Random.Range( -0.1f, 0.1f ), () =>
            {
                this.InterpolatePosition( tile.transform, show ? originPos : exitPosition, menuFadeOutTimeSec + Random.Range( -0.1f, 0.1f ), true, Utility.Easing.Quintic.In );
            } );
        }

        if( show )
        {
            this.FadeToColour( background, Color.black, 1.0f, Utility.Easing.Quintic.In );
            this.FadeToColour( blur, Color.white, 0.5f );
            this.FadeFromTransparent( titleArea, 0.5f );
        }
        else
        {
            this.FadeToColour( background, Color.clear, 0.5f );
            this.FadeToColour( blur, Color.clear, 1.0f );
            this.FadeToTransparent( titleArea, 0.5f );
            yield return new WaitForSeconds( 2.0f );
            gameObject.SetActive( false );
        }
    }

    public override void OnEventReceived( IBaseEvent e )
    {
        if( e is StartGameEvent )
        {
            StartGame( false );
        }
        else if( e is ExitGameEvent )
        {
            Init();
            StartCoroutine( ToggleMenu( true ) );
        }
    }

    public void ToggleFadeText( CanvasGroup fadeOut, CanvasGroup fadeIn )
    {
        fadeOut.gameObject.SetActive( true );
        fadeIn.gameObject.SetActive( true );

        this.TryStopCoroutine( fadeInCoroutine );
        this.TryStopCoroutine( fadeOutCoroutine );

        fadeInCoroutine = StartCoroutine( Utility.FadeFromTransparent( fadeIn, fadeTimeSec ) );
        fadeOutCoroutine = StartCoroutine( Utility.FadeToTransparent( fadeOut, fadeTimeSec, null, true ) );
    }
}
