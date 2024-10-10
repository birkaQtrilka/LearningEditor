using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;

public class MyGrid : MonoBehaviour
{
    const int ROTATIONS = 4;

    [SerializeField] int _seed = 100;
    [SerializeField] float _size = 100f;
    [SerializeField] int columns = 10;

    [SerializeField] Transform _tileHolder;

    [SerializeField] bool _generateNewMap;

    public event Action<Tile> TileCollapsed;
    public UnityEvent MapGenerated;

    public bool Done { get; private set; }
    public Cell[,] Cells { get; private set; }

    int collapsedTileCount;
    float _cellWidth;
    readonly Tile[] _tiles = new Tile[6];
    Random _random;
    IEnumerable<Tile> _rotatedTiles;

    List<CellAndDir> _neighbours = new();

    void Awake()
    {
        _random = new(_seed);
        //hardcoded the simetries.
        //The simetries are needed to save memory on repeating the tiles that after rotation
        //look the same
        //up right down left
        //_tiles[0] = new Tile(_crossPrefab, true, true, "ABA", "ABA", "ABA", "ABA");
        //_tiles[1] = new Tile(_linePrefab, true, false, "AAA", "ABA", "AAA", "ABA");
        //_tiles[2] = new Tile(_tPrefab, false, true, "AAA", "ABA", "ABA", "ABA");
        //_tiles[3] = new Tile(_cornerPrefab, false, false, "ABA", "ABA", "AAA", "AAA");
        //_tiles[4] = new Tile(_blankPrefab, true, true, "AAA", "AAA", "AAA", "AAA");
        //_tiles[5] = new Tile(_capPrefab, false, false, "AAA", "AAA", "AAA", "ABA");

        var rotatedTiles = GenerateRotatedTileStates(_tiles);
        _rotatedTiles = _tiles.Concat(rotatedTiles);

        GenerateMap(_rotatedTiles);
        MapGenerated?.Invoke();

    }

    void Update()
    {
        if (_generateNewMap)
        {
            _generateNewMap = false;
            DestroyMap();
            GenerateMap(_rotatedTiles);
            MapGenerated?.Invoke();

        }
    }

    void GenerateMap(IEnumerable<Tile> rotatedTiles)
    {
        PopulateGrid(columns, rotatedTiles);
        FillGridEdgesWithEmptyTiles(columns);

        while (!Done)
        {
            Iterate();
        }

        ConnectTiles();
    }

    void DestroyMap()
    {
        Done = false;
        collapsedTileCount = 0;

        GameObject newTileHolder = new("Tiles");
        Destroy(_tileHolder.gameObject);
        _tileHolder = newTileHolder.transform;
        _tileHolder.parent = transform;

        Cells = new Cell[columns, columns];
        _cellWidth = _size / columns;
    }

    void Iterate()
    {
        Cell lowestCell = GetLeastEntropyCell();
        if (lowestCell.Possibilities.Count == 0)
        {
            Done = true;
            return;
        }
        RandomCollapseCell(lowestCell);
        Propagate(lowestCell);
    }

    void FillGridEdgesWithEmptyTiles(int columns)
    {
        int emptyTileIndex = _tiles.Length - 2;//hard coded to be empty tile
        Tile emptyTile = _tiles[emptyTileIndex];
        for (int i = 0, y = 0; i < 2; i++, y += columns - 1)//horizontal edges
            for (int x = 0; x < columns; x++)
                PrePlaceTile(x, y, emptyTile);

        for (int i = 0, x = 0; i < 2; i++, x += columns - 1)//vertical edges
            for (int y = 1; y < columns - 1; y++)
                PrePlaceTile(x, y, emptyTile);
    }

    void PrePlaceTile(int gridX, int gridY, Tile tile)
    {
        Cell cell = Cells[gridY, gridX];
        CollapseCell(cell, tile);
        Propagate(cell);
    }

    Cell GetLeastEntropyCell()
    {
        Cell min = null;
        foreach (Cell cell in Cells)
        {
            if (cell.CollapsedTile != null) continue;//is a collapsed cell
            min ??= cell;

            if (cell.Possibilities.Count < min.Possibilities.Count)
                min = cell;
        }
        return min;
    }

    void PopulateGrid(int columns, IEnumerable<Tile> statesList)
    {
        for (int y = 0; y < columns; y++)
            for (int x = 0; x < columns; x++)
            {
                Cells[y, x] = new Cell(x, y, new List<Tile>(statesList));
            }
    }

    List<Tile> GenerateRotatedTileStates(Tile[] unrotatedTiles)
    {
        List<Tile> rotatedTiles = new List<Tile>();

        foreach (Tile tile in unrotatedTiles)
            for (int i = 1; i < ROTATIONS; i++)
            {
                tile.GenerateRotatedVersions(rotatedTiles, i);
            }

        return rotatedTiles;
    }

    void RandomCollapseCell(Cell cell)
    {
        int randomIndex = GetRandomPossibility(cell);
        Tile prototype = cell.Possibilities[randomIndex];
        CollapseCell(cell, prototype);
    }

    int GetRandomPossibility(Cell cell)
    {
        //if there are more than 1 possibility, chose everything except cap tile
        //else choose cap tile
        //I don't want the city to have dead ends unless there are no other possible configurations
        int randomIndex = _random.Next(0, cell.Possibilities.Count);
        if (cell.Possibilities.Count > 1)
        {
            while (IsCapTile(randomIndex)) randomIndex = _random.Next(0, cell.Possibilities.Count);
        }

        return randomIndex;
        bool IsCapTile(int index)
        {
            return cell.Possibilities[index].Prefab.name == "Cap";
        }
    }

    void CollapseCell(Cell cell, Tile prototype)
    {
        cell.Possibilities.Clear();
        cell.CollapsedTile = prototype.Clone();
        Tile tile = cell.CollapsedTile;
        tile.ParentCell = cell;

        //The Y direction is negative because I initialy programed the algorithm in GXPengine and I can't bother
        //to figure out how to write the algorithm with positive y
        //var inst = Instantiate(tile.Prefab, transform.position + new Vector3(cell.X * _cellWidth + .5f * _cellWidth, 0, -cell.Y * _cellWidth - .5f * _cellWidth), Quaternion.AngleAxis(tile.Rotation, transform.up), _tileHolder);
        //inst.transform.localScale = Vector3.one * _cellWidth;
        //cell.WorldObj = inst;
        TileCollapsed?.Invoke(tile);

        if (++collapsedTileCount >= Cells.Length) Done = true;
    }

    void Propagate(Cell cell)
    {
        _neighbours.Clear();
        GetNeighbouringCellsAndDirections(cell.X, cell.Y, _neighbours);
        foreach (CellAndDir val in _neighbours)
        {
            Cell neighbour = val.cell;
            if (neighbour.CollapsedTile != null) continue;

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

    public void GetNeighbouringCellsAndDirections(int x, int y, List<CellAndDir> neighbours)
    {
        //I'm checking the bounds of the array
        if (x - 1 >= 0)
            neighbours.Add(new CellAndDir(Cells[y, x - 1], NeighbourDir.Left));
        if (x + 1 < Cells.GetLength(1))
            neighbours.Add(new CellAndDir(Cells[y, x + 1], NeighbourDir.Right));
        if (y - 1 >= 0)
            neighbours.Add(new CellAndDir(Cells[y - 1, x], NeighbourDir.Up));
        if (y + 1 < Cells.GetLength(0))
            neighbours.Add(new CellAndDir(Cells[y + 1, x], NeighbourDir.Down));
    }

    void ConnectTiles()
    {
        foreach (Cell cell in Cells)
        {
            int x = cell.X;
            int y = cell.Y;
            Tile currentTile = cell.CollapsedTile;

            if (x - 1 >= 0)
                ConnectTiles(Cells[y, x - 1], currentTile, NeighbourDir.Left);
            if (x + 1 < Cells.GetLength(1))
                ConnectTiles(Cells[y, x + 1], currentTile, NeighbourDir.Right);
            if (y - 1 >= 0)
                ConnectTiles(Cells[y - 1, x], currentTile, NeighbourDir.Up);
            if (y + 1 < Cells.GetLength(0))
                ConnectTiles(Cells[y + 1, x], currentTile, NeighbourDir.Down);


        }

        static void ConnectTiles(Cell neighbour, Tile currentTile, NeighbourDir dir)
        {
            Tile neighbourTile = neighbour.CollapsedTile;

            if (currentTile.CanConnectWithBlank(neighbourTile, dir))
            {
                if (!neighbourTile.Neighbours.Contains(currentTile))
                    neighbourTile.Neighbours.Add(currentTile);
                if (!currentTile.Neighbours.Contains(neighbourTile))
                    currentTile.Neighbours.Add(neighbourTile);
            }
        }
    }

    

    
    #region BFS version

    //void ConnectAllTiles(int startX, int startY)
    //{
    //    HashSet<Cell> _visitedNodes = new HashSet<Cell>();
    //    Queue<Cell> _queue = new Queue<Cell>();

    //    Cell startCell = Cells[startY, startX];

    //    if (startCell.CollapsedTile == null) return;

    //    _queue.Enqueue(startCell);
    //    _visitedNodes.Add(startCell);

    //    while (_queue.Count > 0)
    //    {
    //        Cell currentCell = _queue.Dequeue();
    //        Tile currentTile = currentCell.CollapsedTile;
    //        int x = currentCell.X;
    //        int y = currentCell.Y;
    //        if (x - 1 >= 0)
    //            ConnectTiles(Cells[y, x - 1], currentTile, (int)NeighbourDir.Left);
    //        if (x + 1 < Cells.GetLength(1))
    //            ConnectTiles(Cells[y, x + 1], currentTile, (int)NeighbourDir.Right);
    //        if (y - 1 >= 0)
    //            ConnectTiles(Cells[y - 1, x], currentTile, (int)NeighbourDir.Up);
    //        if (y + 1 < Cells.GetLength(0))
    //            ConnectTiles(Cells[y + 1, x], currentTile, (int)NeighbourDir.Down);

    //    }


    //    void ConnectTiles(Cell neighbourCell, Tile currentTile, int dir)
    //    {

    //        Tile neighbourTile = neighbourCell.CollapsedTile;
    //        string currentSockets = currentTile.Sockets[dir];

    //        if (currentSockets != "AAA")//I know that it can connect, I should change it so I take advantage of the fact
    //        {
    //            if (!neighbourTile.Neighbours.Contains(currentTile))
    //                neighbourTile.Neighbours.Add(currentTile);
    //            if (!currentTile.Neighbours.Contains(neighbourTile))
    //                currentTile.Neighbours.Add(neighbourTile);
    //        }
    //        if (_visitedNodes.Contains(neighbourCell)) return;

    //        _queue.Enqueue(neighbourCell);
    //        _visitedNodes.Add(neighbourCell);
    //    }
    //}

    //bool CanConnect(string[] socketsA, string socketsB, int dir)
    //{
    //    return socketsA[(dir + 2) % 4] == socketsB;
    //}
    #endregion


}
public readonly struct CellAndDir
{
    public readonly Cell cell;
    public readonly NeighbourDir dir;

    public CellAndDir(Cell cell, NeighbourDir dir)
    {
        this.cell = cell;
        this.dir = dir;
    }
}