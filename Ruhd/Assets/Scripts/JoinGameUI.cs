using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class JoinGameUI : MonoBehaviour
{
    [SerializeField] GameObject loadingScreen;
    [SerializeField] TMPro.TextMeshProUGUI errorText;
    [SerializeField] TMPro.TMP_InputField nameInput;
    [SerializeField] TMPro.TMP_InputField codeInput;
    [SerializeField] UnityEvent onConfirm;

    public async void TryJoinGame()
    {
        bool valid = true;

        if( nameInput.text.Length == 0 )
        {
            var image = nameInput.GetComponent<Image>();
            image.color = Color.red;
            Utility.FunctionTimer.CreateOrUpdateTimer( 1.0f, () => image.color = Color.white, "Color1" );
            DisplayError( "PLEASE ENTER A NAME" );
            valid = false;
        }

        if( codeInput.text.Length == 0 || codeInput.text.Length < codeInput.characterLimit )
        {
            var image = codeInput.GetComponent<Image>();
            image.color = Color.red;
            Utility.FunctionTimer.CreateOrUpdateTimer( 1.0f, () => image.color = Color.white, "Color2" );
            if( valid )
                DisplayError( codeInput.text.Length == 0
                    ? "PLEASE ENTER A VALID JOIN CODE"
                    : $"CODE MUST BE {codeInput.characterLimit} CHARACTERS" );
            valid = false;
        }

        var rateLimiter = NetworkManager.Singleton.GetComponent<NetworkHandler>().lobbyRateLimiter;
        if( valid && !rateLimiter.CheckLimit() )
        {
            DisplayError( "MAX REQUESTS REACHED - PLEASE WAIT" );
        }

        if( valid )
        {
            await rateLimiter.WaitForCallAsync();

            // show loading screen
            loadingScreen.SetActive( true );

            var result = await NetworkManager.Singleton.GetComponent<NetworkHandler>().JoinLobby( codeInput.text, nameInput.text );

            // hide loading screen
            loadingScreen.SetActive( false );

            // Result 
            if( result == null )
            {
                onConfirm?.Invoke();
            }
            else
            {
                DisplayError( "FAILED TO JOIN GAME: " + result.Message );
            }
        }
    }

    private void DisplayError( string message )
    {
        var canvas = errorText.GetComponent<CanvasGroup>();
        canvas.alpha = 1.0f;
        errorText.gameObject.SetActive( true );
        errorText.text = message.ToUpper();
        Utility.FunctionTimer.CreateOrUpdateTimer( 5.0f, () => this.FadeToTransparent( canvas, 0.5f, null, true ), "JoinGameErrorFade" );
    }

    private void Update()
    {
        if( Input.GetKeyDown( KeyCode.Return ) )
            TryJoinGame();
    }
}
