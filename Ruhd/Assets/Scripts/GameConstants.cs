using UnityEngine;

[CreateAssetMenu( fileName = "GameConstants", menuName = "ScriptableObjs/GameConstants" )]
public class GameConstants : ScriptableObject
{
    public int rngSeed = 0; // 0 means non-fixed/random
    public int deckNumStartingCards = 4;
    public int oneSideExtraScore = 1;
    public int patternExtraScore = 2;
}