using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuPlayTileUI : MonoBehaviour
{
    public void Play()
    {
        EventSystem.Instance.TriggerEvent( new RequestStartGameEvent() );
    }
}
