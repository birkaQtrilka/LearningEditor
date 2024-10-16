using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NeighbourDir
{
    Up,
    Right,
    Down,
    Left,
}

[Serializable]
public class Tile 
{
    //sockets are the edges of tiles divided into 3 parts. These three parts are then checked with other tiles to see if they can be connected
    //A is empty
    //B is road
    [SerializeField] Sockets _sockets;
    public Sockets Sockets => _sockets;
    [field: SerializeField] public GameObject Prefab { get; private set; }
    //runtime
    public float Rotation { get; private set; }
    public List<Tile> Neighbours { get; } = new List<Tile>();

    public Tile()
    {
        
    }
    private Tile(GameObject prefab, float rotation, Sockets sockets) 
    {
        Rotation = rotation;
        _sockets = sockets;
        Prefab = prefab;
    }

    public void Rotate()
    {
        _sockets.Rotate();
        Rotation += 90;
    }

    public Tile Clone()
    {
        Tile nt = new(Prefab, Rotation, _sockets.Clone());
        return nt;
    }

    public bool CanConnectWithBlank(Tile otherTile, NeighbourDir dir)
    {
        if (_sockets.IsBlank(dir, out string mySockets)) return false;

        NeighbourDir oppositeDir = GetOppositeDir(dir);
        string otherSockets = otherTile._sockets.GetSocket(oppositeDir);

        return otherSockets == mySockets;//reverse this
    }

    public bool CanConnect(Tile otherTile, NeighbourDir dir)
    {
        string mySockets = _sockets.GetSocket(dir);
        NeighbourDir oppositeDir = GetOppositeDir(dir);
        string otherSockets = otherTile._sockets.GetSocket(oppositeDir);

        return otherSockets == mySockets;//reverse this
    }

    public static NeighbourDir GetOppositeDir(NeighbourDir dir)
    {
        return dir switch
        {
            NeighbourDir.Up => NeighbourDir.Down,
            NeighbourDir.Right => NeighbourDir.Left,
            NeighbourDir.Down => NeighbourDir.Up,
            NeighbourDir.Left => NeighbourDir.Right,
            _ => dir,
        };
    }

    public void GenerateRotatedVersions(List<Tile> results, int rotations)
    {
        //the odd/even checks are because the symetric versions of tiles will be one array slot appart
        //(180 degrees since every array slot is a 90 degree rotation) 
        //if ((tile.SymetryHorizontal && i % 2 == 0) || (tile.SymetryVertical && i % 2 != 0))
        //    continue;

        Tile newTile = Clone();
        for (int i = 0; i < rotations; i++)
            newTile.Rotate();

        results.Add(newTile);
    }
}
[Serializable]
public class Sockets
{
    //up right down left
   [SerializeField] string[] _edges = new string[4];
    public string[] GetArray()
    {
        return _edges;
    }

    public void Rotate()
    {
        string lastSocket = _edges[^1];
        for (int i = _edges.Length - 1; i >= 1; i--)
        {
            _edges[i] = _edges[i - 1];
        }

        _edges[0] = lastSocket;
    }

    public string GetSocket(NeighbourDir direction)
    {
        return _edges[(int)direction];
    }

    public bool IsBlank(NeighbourDir direction, out string socket)
    {
        socket = GetSocket(direction);
        return socket.All(s => s == 'a');
    }

    public Sockets Clone()
    {
        Sockets nt = new()
        {
            _edges = (string[])_edges.Clone()
        };
        return nt;
    }

    public override string ToString()
    {
        return $"Up: {_edges[0]}, Right: {_edges[1]}, Down: {_edges[2]}, Left: {_edges[3]}";
    }

    
}