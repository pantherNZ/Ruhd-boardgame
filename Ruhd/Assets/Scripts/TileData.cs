using System;
using UnityEngine;

[Serializable]
public class TileSide
{
    public TileSide( TileData card, int value, int colour, Side side)
    {
        this.card = card;
        this.value = value;
        this.colour = colour;
        this.side = side;
        this.patternUsed = false;
    }

    public int value;
    public int colour;
    public TileData card;
    public bool patternUsed;

    public Side rotation { get => side.Rotate( card.owningComponent.rotation ); }
    public Side side;
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
    public static int Value( this Side side )
    {
        return ( int )side;
    }

    public static Side Opposite( this Side side )
    {
        return ( Side )Utility.Mod( side.Value() + 2, 4 );
    }

    public static Side Rotate( this Side side, Side rotate )
    {
        return ( Side )Utility.Mod( side.Value() + rotate.Value(), 4 );
    }
}

[Serializable]
public class TileData : ScriptableObject
{
    // Clockwise ordering
    public TileSide[] sides = new TileSide[4];
    public string imagePath;
    public TileComponent owningComponent;
}