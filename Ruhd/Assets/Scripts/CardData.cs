using System;
using UnityEngine;

[Serializable]
public struct CardSide
{
    public int value;
    public int colour;
}

public enum Side
{
    Up,
    Right,
    Down,
    Left,
}

[Serializable]
[CreateAssetMenu( fileName = "CardTile", menuName = "ScriptableObjs/CardTile" )]
public class CardData : ScriptableObject
{
    // Clockwise ordering
    public CardSide[] sides = new CardSide[4];
}