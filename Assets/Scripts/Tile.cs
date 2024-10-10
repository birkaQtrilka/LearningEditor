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
    [field: SerializeField] public Sockets Socketss { get; set; }
    [field: SerializeField] public GameObject Prefab { get; private set; }
    
    //runtime
    public Cell ParentCell { get; set; } //maybeChangeLater?
    public float Rotation { get; private set; }
    public List<Tile> Neighbours { get; } = new List<Tile>();


    private Tile(GameObject prefab, float rotation, Sockets sockets) 
    {
        Rotation = rotation;
        Socketss = sockets;
        Prefab = prefab;
        //SymetryHorizontal = false;
        //SymetryVertical = false;
    }

    public void Rotate()
    {
        Socketss.Rotate();
        Rotation += 90;
    }

    public Tile Clone()
    {
        Tile nt = new(Prefab, Rotation, Socketss.Clone());
        return nt;
    }

    public bool CanConnectWithBlank(Tile otherTile, NeighbourDir dir)
    {
        if (Socketss.IsBlank(dir, out string mySockets)) return false;

        NeighbourDir oppositeDir = GetOppositeDir(dir);
        string otherSockets = otherTile.Socketss.GetSocket(oppositeDir);

        return otherSockets == mySockets;//reverse this
    }

    public bool CanConnect(Tile otherTile, NeighbourDir dir)
    {
        string mySockets = Socketss.GetSocket(dir);
        NeighbourDir oppositeDir = GetOppositeDir(dir);
        string otherSockets = otherTile.Socketss.GetSocket(oppositeDir);

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
    public string Up;
    public string Right;
    public string Down;
    public string Left;
    string[] array = new string[4];

    public string[] GetArray()
    {
        array[0] = Up;
        array[1] = Right;
        array[2] = Down;
        array[3] = Left;
        return array;
    }

    public void Rotate()
    {
        string lastSocket = array[^1];
        for (int i = array.Length - 1; i >= 1; i--)
        {
            array[i] = array[i - 1];
        }

        array[0] = lastSocket;
        Up = array[0];
        Right = array[1];
        Down = array[2];
        Left = array[3];
    }

    public string GetSocket(NeighbourDir direction)
    {
        return direction switch
        {
            NeighbourDir.Up => Up,
            NeighbourDir.Right => Right,
            NeighbourDir.Down => Down,
            NeighbourDir.Left => Left,
            _ => null,
        };
    }

    public bool IsBlank(NeighbourDir direction, out string socket)
    {
        socket = GetSocket(direction);
        return socket.All(s => s == 'a');
    }

    public Sockets Clone()
    {
        Sockets nt = new();
        nt.Up = Up;
        nt.Right = Right;
        nt.Down = Down;
        nt.Left = Left;
        nt.array = (string[])array.Clone();
        return nt;
    }
}