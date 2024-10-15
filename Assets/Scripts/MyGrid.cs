using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;

public class MyGrid : MonoBehaviour
{
    const int ROTATIONS = 4;

    [SerializeField] float _size = 100f;

    [SerializeField] Transform _tileHolder;

    [SerializeField] bool _generateNewMap;

    public event Action<Tile> TileCollapsed;
    public UnityEvent MapGenerated;

    
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
    public readonly GridCell cell;
    public readonly NeighbourDir dir;

    public CellAndDir(GridCell cell, NeighbourDir dir)
    {
        this.cell = cell;
        this.dir = dir;
    }
}