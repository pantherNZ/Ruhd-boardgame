using Unity.Netcode;
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
            Utility.FunctionTimer.StopTimer( "Color1" );
            Utility.FunctionTimer.CreateTimer( 1.0f, () => image.color = Color.white, "Color1" );
            valid = false;
        }

        if( codeInput.text.Length == 0  )
        {
            var image = codeInput.GetComponent<Image>();
            image.color = Color.red;
            Utility.FunctionTimer.StopTimer( "Color2" );
            Utility.FunctionTimer.CreateTimer( 1.0f, () => image.color = Color.white, "Color2" );
            valid = false;
        }

        if( valid )
        {
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
                var canvas = errorText.GetComponent<CanvasGroup>();
                canvas.alpha = 1.0f;
                errorText.gameObject.SetActive( true );
                errorText.text = "FAILED TO JOIN GAME: " + result.Message;
                Utility.FunctionTimer.CreateTimer( 5.0f, () => this.FadeToTransparent( canvas, 0.5f, null, true ) );
            }
        }
    }
}
