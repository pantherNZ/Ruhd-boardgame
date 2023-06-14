using System;
using UnityEngine;

[Serializable]
public struct CardSide
{
    public CardSide(int v, int c)
    {
        value = v;
        colour = c;
    }

    public int value;
    public int colour;
}

[Serializable]
public enum Side
{
    Up,
    Right,
    Down,
    Left,
    NumSides,
}

[Serializable]
public class CardData : ScriptableObject
{
    // Clockwise ordering
    public CardSide[] sides = new CardSide[4];
}