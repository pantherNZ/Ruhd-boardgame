using System;
using UnityEngine;

[Serializable]
public class TileSide
{
    public TileSide( TileData card, int value, int colour)
    {
        this.card = card;
        this.value = value;
        this.colour = colour;
        this.patternUsed = false;
    }

    public int value;
    public int colour;
    public TileData card;
    public bool patternUsed;
}

[Serializable]
public enum Side
{
    Up,
    Right,
    Down,
    Left,
}

static class SideUtil
{
    public static Side Opposite( this Side side )
    {
        return ( Side )Utility.Mod( ( int )side + 2, 4 );
    }

    public static int Value( this Side side )
    {
        return ( int )side;
    }
}

[Serializable]
public class TileData : ScriptableObject
{
    // Clockwise ordering
    public TileSide[] sides = new TileSide[4];
    public string imagePath;
}