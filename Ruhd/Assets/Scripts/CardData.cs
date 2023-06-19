using System;
using UnityEngine;

[Serializable]
public struct CardSide
{
    public CardSide( CardData card, int value, int colour)
    {
        this.card = card;
        this.value = value;
        this.colour = colour;
    }

    public int value;
    public int colour;
    public CardData card;
}

[Serializable]
public enum Side
{
    Up,
    Right,
    Down,
    Left,
}

[Serializable]
public class CardData : ScriptableObject
{
    // Clockwise ordering
    public CardSide[] sides = new CardSide[4];
    public string imagePath;
}