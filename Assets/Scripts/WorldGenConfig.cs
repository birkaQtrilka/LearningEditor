using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName ="Stefan/WorldData", fileName ="new WorldGenConfig")]
public class WorldGenConfig : ScriptableObject
{
    public int Columns;
    public int SocketsCount;
    public List<Tile> AvailableTiles = new();
    public GridCellCollection Grid;


}


[Serializable]
public class GridCellCollection
{

    [SerializeField] GridCellRow[] cellRows;

    public GridCellCollection(int columns)
    {
        cellRows = new GridCellRow[columns];
        for (int i = 0; i < cellRows.Length; i++) 
        {
            cellRows[i] = new GridCellRow(columns);
        }
    }


    public GridCell this[int indexY, int indexX]
    {
        get => cellRows[indexY].cells[indexX];
        set => cellRows[indexY].cells[indexX] = value;
    }

    public int GetVerticalLength()
    {
        return cellRows.Length;
    }
    public int GetHorizontalLength()
    {
        return cellRows.Length == 0 ? -1 : cellRows[0].cells.Length;
    }
}

[Serializable]
public class GridCellRow
{
    public GridCell[] cells;
    public GridCellRow(int columns)
    {
        cells = new GridCell[columns];
    }
}

[Serializable]
public class GridCell
{
    public int PopUpIndex;
    public Tile tile = null;
}