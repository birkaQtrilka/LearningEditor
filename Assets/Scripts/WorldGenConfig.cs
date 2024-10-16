using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
[CreateAssetMenu(menuName ="Stefan/WorldData", fileName ="new WorldGenConfig")]
public class WorldGenConfig : ScriptableObject
{
    const int ROTATIONS = 4;

    public int Columns;
    public int SocketsCount;
    public List<Tile> AvailableTiles = new();
    public GridCellCollection Grid;

    [SerializeField] int _seed = 100;
    List<CellAndDir> _neighbours = new();
    System.Random _random;

    bool Done;
    int collapsedTileCount;

    public void DestroyMap()
    {
        Done = false;
        collapsedTileCount = 0;

        Grid = new GridCellCollection(Columns);
    }

    public void GenerateGrid()
    {
        _random = new System.Random(_seed);
        IEnumerable<Tile> rotatedTiles = AvailableTiles.Concat(GenerateRotatedTileStates(AvailableTiles));

        PopulateGrid(rotatedTiles);
        //FillGridEdgesWithEmptyTiles(columns);

        while (!Done)
        {
            Iterate();
        }

        ConnectTiles();
    }

    List<Tile> GenerateRotatedTileStates(List<Tile> unrotatedTiles)
    {
        List<Tile> rotatedTiles = new();

        foreach (Tile tile in unrotatedTiles)
            for (int i = 1; i < ROTATIONS; i++)
            {
                tile.GenerateRotatedVersions(rotatedTiles, i);
            }

        return rotatedTiles;
    }

    void PopulateGrid(IEnumerable<Tile> statesList)
    {
        for (int y = 0; y < Columns; y++)
            for (int x = 0; x < Columns; x++)
            {
                Grid[y, x].Init(x, y,0,new List<Tile>(statesList));
            }
    }

    void Iterate()
    {
        GridCell lowestCell = GetLeastEntropyCell();
        if (lowestCell.Possibilities.Count == 0)
        {
            Done = true;
            return;
        }
        RandomCollapseCell(lowestCell);
        Propagate(lowestCell);
    }

    GridCell GetLeastEntropyCell()
    {
        GridCell min = null;
        for (int y = 0; y < Columns; y++)
            for (int x = 0; x < Columns; x++)
            {
                GridCell cell = Grid[y, x];
                if (!cell.IsEmpty()) continue;//is a collapsed cell
                min ??= cell;

                if (cell.Possibilities.Count < min.Possibilities.Count)
                    min = cell;
            }
        return min;
    }

    void RandomCollapseCell(GridCell cell)
    {
        int randomIndex = GetRandomPossibility(cell);
        Tile prototype = cell.Possibilities[randomIndex];
        CollapseCell(cell, prototype);
    }

    int GetRandomPossibility(GridCell cell)
    {
        //if there are more than 1 possibility, chose everything except cap tile
        //else choose cap tile
        //I don't want the city to have dead ends unless there are no other possible configurations
        int randomIndex = _random.Next(0, cell.Possibilities.Count);
        //if (cell.Possibilities.Count > 1)
        //{
        //    while (IsCapTile(randomIndex)) randomIndex = _random.Next(0, cell.Possibilities.Count);
        //}

        return randomIndex;
        //bool IsCapTile(int index)
        //{
        //    return cell.Possibilities[index].Prefab.name == "Cap";
        //}
    }

    void CollapseCell(GridCell cell, Tile prototype)
    {
        cell.Possibilities.Clear();
        cell.tile = prototype.Clone();
        cell.PopUpIndex = GetPopUpIndex(cell.tile);
        //The Y direction is negative because I initialy programed the algorithm in GXPengine and I can't bother
        //to figure out how to write the algorithm with positive y
        //var inst = Instantiate(tile.Prefab, transform.position + new Vector3(cell.X * _cellWidth + .5f * _cellWidth, 0, -cell.Y * _cellWidth - .5f * _cellWidth), Quaternion.AngleAxis(tile.Rotation, transform.up), _tileHolder);
        //inst.transform.localScale = Vector3.one * _cellWidth;
        //cell.WorldObj = inst;

        if (++collapsedTileCount >= Grid.Length) Done = true;
    }

    void Propagate(GridCell cell)
    {
        _neighbours.Clear();
        GetNeighbouringCellsAndDirections(cell.X, cell.Y, _neighbours);
        foreach (CellAndDir val in _neighbours)
        {
            GridCell neighbour = val.cell;
            if (!neighbour.IsEmpty()) continue;

            //constrain
            for (int i = 0; i < neighbour.Possibilities.Count; i++)
            {
                List<Tile> possibilities = neighbour.Possibilities;
                Tile possibility = possibilities[i];
                //the modulo operation is to overlap values, the addition to two is because the opposite side of cell is 2 array slots appart
                if (!possibility.CanConnectWithBlank(possibility, val.dir))
                    possibilities.RemoveAt(i--);
            }
        }
    }

    void GetNeighbouringCellsAndDirections(int x, int y, List<CellAndDir> neighbours)
    {
        //I'm checking the bounds of the array
        if (x - 1 >= 0)
            neighbours.Add(new CellAndDir(Grid[y, x - 1], NeighbourDir.Left));
        if (x + 1 < Grid.GetHorizontalLength())
            neighbours.Add(new CellAndDir(Grid[y, x + 1], NeighbourDir.Right));
        if (y - 1 >= 0)
            neighbours.Add(new CellAndDir(Grid[y - 1, x], NeighbourDir.Up));
        if (y + 1 < Grid.GetVerticalLength())
            neighbours.Add(new CellAndDir(Grid[y + 1, x], NeighbourDir.Down));
    }

    void ConnectTiles()
    {
        for (int y = 0; y < Columns; y++)
            for (int x = 0; x < Columns; x++)
            {
                Tile currentTile = Grid[y,x].tile;

                if (x - 1 >= 0)
                    ConnectTiles(Grid[y, x - 1], currentTile, NeighbourDir.Left);
                if (x + 1 < Grid.GetHorizontalLength())
                    ConnectTiles(Grid[y, x + 1], currentTile, NeighbourDir.Right);
                if (y - 1 >= 0)
                    ConnectTiles(Grid[y - 1, x], currentTile, NeighbourDir.Up);
                if (y + 1 < Grid.GetVerticalLength())
                    ConnectTiles(Grid[y + 1, x], currentTile, NeighbourDir.Down);

            }
        static void ConnectTiles(GridCell neighbour, Tile currentTile, NeighbourDir dir)
        {
            Tile neighbourTile = neighbour.tile;

            if (currentTile.CanConnectWithBlank(neighbourTile, dir))
            {
                if (!neighbourTile.Neighbours.Contains(currentTile))
                    neighbourTile.Neighbours.Add(currentTile);
                if (!currentTile.Neighbours.Contains(neighbourTile))
                    currentTile.Neighbours.Add(neighbourTile);
            }
        }
    }

    int GetPopUpIndex(Tile tile)
    {
        Debug.Assert(tile != null, "you should always find the prefab, check if the prefab is null");

        if (tile == null) return -1;
        for (int i = 0; i < AvailableTiles.Count; i++)
        {
            if (tile.Prefab == AvailableTiles[i].Prefab)
                return i + 1;
        }
        return -1;
    }
    //void FillGridEdgesWithEmptyTiles(int columns)
    //{
    //    int emptyTileIndex = _tiles.Length - 2;//hard coded to be empty tile
    //    Tile emptyTile = _tiles[emptyTileIndex];
    //    for (int i = 0, y = 0; i < 2; i++, y += columns - 1)//horizontal edges
    //        for (int x = 0; x < columns; x++)
    //            PrePlaceTile(x, y, emptyTile);

    //    for (int i = 0, x = 0; i < 2; i++, x += columns - 1)//vertical edges
    //        for (int y = 1; y < columns - 1; y++)
    //            PrePlaceTile(x, y, emptyTile);
    //}
}


[Serializable]
public class GridCellCollection
{

    [SerializeField] GridCellRow[] _cellRows;
    public int Length => _cellRows[0].Cells.Length * _cellRows.Length;
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
public class GridCell 
{
    public int PopUpIndex;//this should be influenced when setting the tile
    public List<Tile> Possibilities;
    public int X;
    public int Y;
    public Tile tile = null;

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