using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HostGameUI : MonoBehaviour
{
    [SerializeField] TMPro.TMP_InputField nameInput;
    [SerializeField] UnityEvent onConfirm;

    public void TryHostGame()
    {
        if( nameInput.text.Length == 0 )
        {
            var image = nameInput.GetComponent<Image>();
            image.color = Color.red;
            Utility.FunctionTimer.StopTimer( "Color" );
            Utility.FunctionTimer.CreateTimer( 1.0f, () => image.color = Color.white, "Color" );
            return;
        }

        onConfirm?.Invoke();
    }
}
