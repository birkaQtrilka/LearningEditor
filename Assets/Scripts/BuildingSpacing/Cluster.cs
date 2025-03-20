using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Cluster
{
    public Dictionary<int, BuildCell> Cells { get; private set; } = new();
    public MinMax MinMax;
    public int MinimumHousePerimeter;

    readonly Queue<House> _toDoHouses = new();
    readonly List<House> _houses = new();

    readonly System.Random _random;
    readonly int MinHouses;
    readonly int MaxArea;
    public readonly int ID;
    UnityEngine.Color _debugClr;

    public Cluster(int minimumHousePerimeter, System.Random random, int id,  int minHouses = 2, int maxArea = 1000)
    {
        MinimumHousePerimeter = minimumHousePerimeter;
        _random = random;
        MinHouses = minHouses;
        MaxArea = maxArea;
        ID = id;
        MinMax = new MinMax(true);
        _debugClr  = UnityEngine.Random.ColorHSV(0,1);
    }

    public bool ContainsKey(int k)
    {
        return Cells.ContainsKey(k);
    }

    public void Add(int k, BuildCell cell)
    {
        Cells.Add(k, cell);
    }

    public void Remove(int k)
    {
        Cells.Remove(k);
    }

    public MinMax fake(MinMax minMax)
    {
        var copy = MinMax;

        if (minMax.MinX < MinMax.MinX) copy.MinX = minMax.MinX;
        if (minMax.MaxX > MinMax.MaxX) copy.MaxX = minMax.MaxX;

        if (minMax.MinY < MinMax.MinY) copy.MinY = minMax.MinY;
        if (minMax.MaxY > MinMax.MaxY) copy.MaxY = minMax.MaxY;
        return copy;
    }

    public void UpdateMinMax(Vector2Int gridPos)
    {
        if (gridPos.x < MinMax.MinX) MinMax.MinX = gridPos.x;
        if (gridPos.x > MinMax.MaxX) MinMax.MaxX = gridPos.x;

        if (gridPos.y < MinMax.MinY) MinMax.MinY = gridPos.y;
        if (gridPos.y > MinMax.MaxY) MinMax.MaxY = gridPos.y;
    }

    public void UpdateMinMax(MinMax minMax)
    {
        if (minMax.MinX < MinMax.MinX) MinMax.MinX = minMax.MinX;
        if (minMax.MaxX > MinMax.MaxX) MinMax.MaxX = minMax.MaxX;

        if (minMax.MinY < MinMax.MinY) MinMax.MinY = minMax.MinY;
        if (minMax.MaxY > MinMax.MaxY) MinMax.MaxY = minMax.MaxY;
    }
    
    public void GenerateHouses()
    {
        //if(Cells.Count == 0)
        //{
        //    Debug.Log("Won't generate 1x1 cell");
        //    return;
        //}

        int splits = 0;
        House startingCanvas = new
        (
            new Rectangle
            (
                x: MinMax.MinX, 
                y: MinMax.MinY, 
                width: MinMax.MaxX - MinMax.MinX,
                height: MinMax.MaxY - MinMax.MinY
            )
        );
        _toDoHouses.Enqueue( startingCanvas );
        while (_toDoHouses.Count > 0)
        {
            House house = _toDoHouses.Dequeue();
            //int splitChance = 8;
            if (splits > MinHouses && house.Rect.Width * house.Rect.Height < MaxArea /*&& _random.Next(0, splitChance) == 1*/)
            {
                _houses.Add(house);
                continue;
            }

            if (house.Rect.Width < house.Rect.Height && CanDivideVertically(house, MinimumHousePerimeter))
            {
                int oneHalf = house.Rect.Height - _random.Next(MinimumHousePerimeter, house.Rect.Height - MinimumHousePerimeter);
                int otherHalf = house.Rect.Height - oneHalf;

                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y,               house.Rect.Width, oneHalf )));
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y + oneHalf + 1,    house.Rect.Width, otherHalf)));
            }
            else if (CanDivideHorizontally(house, MinimumHousePerimeter))
            {
                int oneHalf = house.Rect.Width - _random.Next(MinimumHousePerimeter, house.Rect.Width - MinimumHousePerimeter);
                int otherHalf = house.Rect.Width - oneHalf;
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y,             oneHalf , house.Rect.Height)));
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X + oneHalf + 1, house.Rect.Y,   otherHalf, house.Rect.Height)));

            }
            else
                _houses.Add(house);

            splits += 2;
            //if(true)
            //{
            //    while (_toDoHouses.Count > 0)
            //    {
            //        _houses.Add(_toDoHouses.Dequeue());
            //    }
            //    return;
            //}
            //_houses.Add(_toDoHouses.Dequeue());
            //_houses.Add(_toDoHouses.Dequeue());
            //return;
        }
    }

    bool CanDivideVertically(House house, int pMinimumRoomSize)
    {
        return house.Rect.Height - pMinimumRoomSize >= pMinimumRoomSize;
    }

    bool CanDivideHorizontally(House house, int pMinimumRoomSize)
    {
        return house.Rect.Width - pMinimumRoomSize >= pMinimumRoomSize;
    }

    public void Draw(Transform prefab, Transform container)
    {
        //if (Cells.Count == 0)
        //{
        //    return;
        //}
        var containerInst = GameObject.Instantiate(container);
        containerInst.localPosition = container.localPosition;
        foreach (House item in _houses)
        {
            var inst = GameObject.Instantiate(prefab, containerInst);
            inst.localPosition = new Vector3(item.Rect.X / 3f, 0, item.Rect.Y / 3f);
            inst.localScale = new Vector3(item.Rect.Width /3f, .1f, item.Rect.Height /3f);
        }
        
    }
    public void DrawMinMax()
    {
        //if (Cells.Count == 0)
        //{
        //    return;
        //}


        Gizmos.color = _debugClr;
        Gizmos.DrawLine(new Vector3(MinMax.MinX, 0, MinMax.MinY ), new Vector3(MinMax.MaxX + 1, 0, MinMax.MinY ));
        Gizmos.DrawLine(new Vector3(MinMax.MaxX + 1, 0, MinMax.MinY ), new Vector3(MinMax.MaxX + 1, 0, MinMax.MaxY +1));
        Gizmos.DrawLine(new Vector3(MinMax.MaxX + 1, 0, MinMax.MaxY + 1), new Vector3(MinMax.MinX, 0, MinMax.MaxY + 1));
        Gizmos.DrawLine(new Vector3(MinMax.MinX, 0, MinMax.MaxY + 1), new Vector3(MinMax.MinX, 0, MinMax.MinY ));
    }
}
