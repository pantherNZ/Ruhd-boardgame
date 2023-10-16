using UnityEngine;

[CreateAssetMenu( fileName = "GameConstants", menuName = "ScriptableObjs/GameConstants" )]
public class GameConstants : ScriptableObject
{
    public int rngSeed = 0; // 0 means non-fixed/random
    public int rngSeedRuntime = 0;
    public int oneSideExtraScore = 1;
    public int patternExtraScore = 2;
    public int patternLengthMin = 3;
    public float tileRotationInterpSec = 0.2f;
    public float challengeStartTimerSec = 5.0f;
    public float challengeStartTimerSecLocal = 2.0f;
    public float challengeActionTimerSec = 10.0f;

    private static GameConstants _Instance;
    public static GameConstants Instance
    {
        get
        {
            if( _Instance == null )
            {
                var instances = Resources.LoadAll<GameConstants>( "Data/GameConstants" );
                if( instances == null || instances.Length < 1)
                    Debug.LogError( "Failed to find GameConstants scriptable object instance for singleton assigning" );
                else if(instances.Length > 1)
                    Debug.LogError( "Multiple GameConstants scriptable object instances exist (should only be one)" );
                else
                    _Instance = instances[0];

                // Init data
                _Instance.rngSeedRuntime = _Instance.rngSeed == 0 ? Random.Range( 0, int.MaxValue ) : _Instance.rngSeed;
            }
            return _Instance;
        }
    }
}