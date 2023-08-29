using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class JoinGameUI : MonoBehaviour
{
    [SerializeField] TMPro.TMP_InputField nameInput;
    [SerializeField] TMPro.TMP_InputField codeInput;
    [SerializeField] UnityEvent onConfirm;

    public void TryJoinGame()
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
            onConfirm?.Invoke();
    }
}
