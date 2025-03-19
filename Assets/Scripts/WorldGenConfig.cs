using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public struct ColorForSocket
{
    public char socket;
    public Color color;
}

[CreateAssetMenu(menuName ="Stefan/WorldData", fileName ="new WorldGenConfig")]
public class WorldGenConfig : ScriptableObject
{
    const int ROTATIONS = 4;

    public int Columns = 4;
    public int SocketsCount = 3;
    public List<Tile> AvailableTiles = new();
    public GridCellCollection Grid = new(5);
    public ColorForSocket[] SocketColors;

    [SerializeField] int _seed = 100;
    [SerializeField] bool _framed;
    [SerializeField] int _framingTileIndex;

    [SerializeField] List<CellAndDir> _neighbours = new();
    [SerializeField] List<GridCell> _prePlacedCells = new();
    [SerializeField, HideInInspector] bool Done;
    System.Random _random;

    public bool drawGrid;
    public bool drawSockets;
    public bool allowHollowTiles;
    public bool drawNames;

    public void DestroyMap()
    {
        Done = false;

        Grid = new GridCellCollection(Columns);
    }

    public void GenerateGrid()
    {
        GenerateGrid(_seed);
        
    }

    public void GenerateGrid(int seed)
    {
        Done = false;
        _random = new System.Random(seed);
        var rotatedTiles = AvailableTiles.Concat(GenerateRotatedTileStates(AvailableTiles)).ToArray();
        _prePlacedCells.Clear();
        //any pre placed tiles should be here for optimization

        PopulateGrid(rotatedTiles);

        FillGridEdgesWithEmptyTiles();

        foreach (GridCell cell in _prePlacedCells)
        {
            Propagate(cell);
        }

        while (!Done)
        {
            Iterate();
        }

        //ConnectTiles();

    }

    void FillGridEdgesWithEmptyTiles()
    {
        Tile emptyTile = AvailableTiles[_framingTileIndex];

        for (int y = 0; y < Columns; y++)
            CollapseCell(Grid[y, 0], emptyTile);
        for (int y = 0; y < Columns; y++)
            CollapseCell(Grid[y, Columns-1], emptyTile);

        for (int x = 1; x < Columns-1; x++)
            CollapseCell(Grid[0, x], emptyTile);
        for (int x = 1; x < Columns-1; x++)
            CollapseCell(Grid[Columns-1, x], emptyTile);
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
                //in editing mode, cells are instantiated automatically, but in playmode I have to do it manually
                Grid[y, x] ??= new GridCell();
                GridCell cell = Grid[y, x];

                if(cell.IsEmpty())
                    cell.Init(x, y, 0, new List<Tile>(statesList));
                else
                {
                    cell.Init(x, y, cell.PopUpIndex, new List<Tile>());
                    _prePlacedCells.Add(cell);
                }

            }
    }

    void Iterate()
    {
        GridCell lowestCell = GetLeastEntropyCell();
        if (lowestCell == null || lowestCell.Possibilities.Count == 0)
        {
            if (lowestCell != null)
                Debug.Log(lowestCell.X + "," + lowestCell.Y);
            Done = true;
            return;
        }
        RandomCollapseCell(lowestCell);
        //Propagate(lowestCell);
    }


    GridCell GetLeastEntropyCell()
    {
        GridCell min = null;
        for (int y = 0; y < Columns; y++)
            for (int x = 0; x < Columns; x++)
            {
                GridCell cell = Grid[y, x];
                if (!cell.IsEmpty() || (cell.Possibilities.Count == 0 && allowHollowTiles)) continue;//is a collapsed cell
                min ??= cell;

                if (cell.Possibilities.Count < min.Possibilities.Count )
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
        if(cell.Possibilities.Count == 1) return 0;

        float totalChance = 0;
        foreach (Tile possibility in cell.Possibilities)
            totalChance += possibility.SpawnChance;

        float rand = (float)_random.NextDouble() * totalChance;
        float cummulativeChance = 0;
        int index = 0;
        
        foreach (Tile possibility in cell.Possibilities)
        {
            cummulativeChance += possibility.SpawnChance;
            if (rand <= cummulativeChance)
                return index;
            index++;
        }
        
        return 0;
    }

    void CollapseCell(GridCell cell, Tile prototype)
    {
        cell.Possibilities.Clear();
        cell.tile = prototype.Clone();
        cell.PopUpIndex = GetPopUpIndex(cell.tile);
        //The Y direction is negative because I initialy programed the algorithm in GXPengine and I can't bother
        //to figure out how to write the algorithm with positive y
        Propagate(cell);
    }

    void Propagate(GridCell cell)
    {
        _neighbours.Clear();
        GetNeighbouringCellsAndDirections(cell.X, cell.Y, _neighbours);
        foreach (CellAndDir val in _neighbours)
        {
            GridCell neighbour = val.cell;
            if (!neighbour.IsEmpty()) continue;
            //have at least one connection and don't allow the 'path' (a preselected letter) to stop if possible
            //constrain
            for (int i = 0; i < neighbour.Possibilities.Count; i++)
            {
                List<Tile> possibilities = neighbour.Possibilities;
                Tile possibility = possibilities[i];
                //the modulo operation is to overlap values, the addition to two is because the opposite side of cell is 2 array slots appart
                if (!cell.tile.CanConnect(possibility, val.dir))
                    possibilities.RemoveAt(i--);
                
            }
        }
    }
    
    //char _pathName = 'c';
    //List<CellAndDir> _cellArray = new(7);

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

    void GetFreeCells(int x, int y, List<CellAndDir> cells)
    {
        void AddIfEmpty(int pX, int pY, NeighbourDir dir)
        {
            GridCell checkingCell = Grid[pY, pX];
            if(checkingCell.IsEmpty()) cells.Add(new(checkingCell,dir));
        }

        if (x - 1 >= 0)                         AddIfEmpty(y, x - 1, NeighbourDir.Left);
        if (x + 1 < Grid.GetHorizontalLength()) AddIfEmpty(y, x + 1, NeighbourDir.Right);
        if (y - 1 >= 0)                         AddIfEmpty(y - 1, x, NeighbourDir.Up);
        if (y + 1 < Grid.GetVerticalLength())   AddIfEmpty(y + 1, x, NeighbourDir.Down);
    }

    //void ConnectTiles()
    //{
    //    for (int y = 0; y < Columns; y++)
    //        for (int x = 0; x < Columns; x++)
    //        {
    //            Tile currentTile = Grid[y,x].tile;

    //            if (x - 1 >= 0)
    //                ConnectTiles(Grid[y, x - 1], currentTile, NeighbourDir.Left);
    //            if (x + 1 < Grid.GetHorizontalLength())
    //                ConnectTiles(Grid[y, x + 1], currentTile, NeighbourDir.Right);
    //            if (y - 1 >= 0)
    //                ConnectTiles(Grid[y - 1, x], currentTile, NeighbourDir.Up);
    //            if (y + 1 < Grid.GetVerticalLength())
    //                ConnectTiles(Grid[y + 1, x], currentTile, NeighbourDir.Down);

    //        }
    //    static void ConnectTiles(GridCell neighbour, Tile currentTile, NeighbourDir dir)
    //    {
    //        Tile neighbourTile = neighbour.tile;

    //        if (currentTile.CanConnectWithBlank(neighbourTile, dir))
    //        {
    //            if (!neighbourTile.Neighbours.Contains(currentTile))
    //                neighbourTile.Neighbours.Add(currentTile);
    //            if (!currentTile.Neighbours.Contains(neighbourTile))
    //                currentTile.Neighbours.Add(neighbourTile);
    //        }
    //    }
    //}

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
public class GridCellRow
{
    public GridCell[] Cells = new GridCell[0];
    public int Length => Cells.Length;

    public GridCellRow(int columns)
    {
        Cells = new GridCell[columns];
    }
}
