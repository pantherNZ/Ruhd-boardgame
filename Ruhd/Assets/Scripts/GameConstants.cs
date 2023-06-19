using UnityEngine;

[CreateAssetMenu( fileName = "GameConstants", menuName = "ScriptableObjs/GameConstants" )]
public class GameConstants : ScriptableObject
{
    public int deckNumStartingCards = 5;
    public int oneSideExtraScore = 1;
    public int patternExtraScore = 2;
}