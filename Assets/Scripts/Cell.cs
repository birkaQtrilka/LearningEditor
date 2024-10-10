using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public List<Tile> Possibilities { get; }
    public int X { get; }
    public int Y { get; }
    public Tile CollapsedTile { get; set; }

    public Cell(int x, int y, List<Tile> possibilities)
    {
        Possibilities = possibilities;
        X = x;
        Y = y;
    }

}
