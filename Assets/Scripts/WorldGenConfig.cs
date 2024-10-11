using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [SerializeField] GridCellRow[] _cellRows;

    public GridCellCollection(int columns)
    {
        _cellRows = new GridCellRow[columns];
        for (int i = 0; i < _cellRows.Length; i++) 
        {
            _cellRows[i] = new GridCellRow(columns);
        }
    }


    public GridCell this[int indexY, int indexX]
    {
        get => _cellRows[indexY].Cells[indexX];
        set => _cellRows[indexY].Cells[indexX] = value;
    }

    public int GetVerticalLength()
    {
        return _cellRows.Length;
    }
    public int GetHorizontalLength()
    {
        return _cellRows.Length == 0 ? -1 : _cellRows[0].Cells.Length;
    }
}

[Serializable]
public class GridCellRow
{
    public GridCell[] Cells;
    public GridCellRow(int columns)
    {
        Cells = new GridCell[columns];
    }
}

[Serializable]
public class GridCell : UnityEngine.Object 
{
    public int PopUpIndex;
    public Tile tile = null;
}