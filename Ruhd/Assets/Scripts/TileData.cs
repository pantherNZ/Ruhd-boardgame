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
    }

    [ReadOnly] public int value;
    [ReadOnly] public int colour;
    [ReadOnly] public TileData card;
    [ReadOnly] public bool patternUsed;

    public Side rotation { get => side.Rotate( card.owningComponent.rotation ); }
    [ReadOnly] public Side side;
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
    [ReadOnly] public TileSide[] sides = new TileSide[4];
    [ReadOnly] public string imagePath;
    [HideInInspector] public TileComponent owningComponent;
}