﻿using System;
using System.Collections.Generic;

[Serializable]
public class GridCell 
{
    public int PopUpIndex;//this should be influenced when setting the tile
    public List<Tile> Possibilities;
    public int X;
    public int Y;
    public Tile tile = null;

    public override string ToString()
    {
        return $"x: {X}, y: {Y},popUpIndex:{PopUpIndex}, possibilites: {Possibilities?.Count}, tile: {tile}";
    }

    public void Init(int x, int y, int popUpIndex, List<Tile> possibilities)
    {
        this.X = x;
        this.Y = y;
        this.PopUpIndex = popUpIndex;
        Possibilities = possibilities;
    }

    public bool IsEmpty()
    {
        return tile.Prefab == null;
    }
}