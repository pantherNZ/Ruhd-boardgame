using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HostGameUI : MonoBehaviour
{
    [SerializeField] GameObject loadingScreen;
    [SerializeField] TMPro.TextMeshProUGUI errorText;
    [SerializeField] TMPro.TMP_InputField nameInput;
    [SerializeField] UnityEvent onConfirm;

    public async void TryHostGame()
    {
        if( nameInput.text.Length == 0 )
        {
            var image = nameInput.GetComponent<Image>();
            image.color = Color.red;
            Utility.FunctionTimer.StopTimer( "Color" );
            Utility.FunctionTimer.CreateTimer( 1.0f, () => image.color = Color.white, "Color" );
            return;
        }

        // show loading screen
        loadingScreen.SetActive( true );
        var result = await NetworkManager.Singleton.GetComponent<NetworkHandler>().HostLobby( nameInput.text );

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
            errorText.text = "FAILED TO HOST GAME: " + result.Message;
            Utility.FunctionTimer.CreateTimer( 5.0f, () => this.FadeToTransparent( canvas, 0.5f, null, true ) );
        }
    }
}
