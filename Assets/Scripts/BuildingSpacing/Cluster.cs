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

    public Cluster(int minimumHousePerimeter, System.Random random, int id,  int minHouses = 2, int maxArea = 1000)
    {
        MinimumHousePerimeter = minimumHousePerimeter;
        _random = random;
        MinHouses = minHouses;
        MaxArea = maxArea;
        ID = id;
        MinMax = new MinMax(true);
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

    public void UpdateMinMax(Vector2Int gridPos)
    {
        if (gridPos.x < MinMax.MinX) MinMax.MinX = gridPos.x;
        else if (gridPos.x > MinMax.MaxX) MinMax.MaxX = gridPos.x;

        if (gridPos.y < MinMax.MinY) MinMax.MinY = gridPos.y;
        else if (gridPos.y > MinMax.MaxY) MinMax.MaxY = gridPos.y;
    }

    public void UpdateMinMax(MinMax minMax)
    {
        if (minMax.MinX < MinMax.MinX) MinMax.MinX = minMax.MinX;
        else if (minMax.MaxX > MinMax.MaxX) MinMax.MaxX = minMax.MaxX;

        if (minMax.MinY < MinMax.MinY) MinMax.MinY = minMax.MinY;
        else if (minMax.MaxY > MinMax.MaxY) MinMax.MaxY = minMax.MaxY;
    }
    
    public void GenerateHouses()
    {
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
        //_houses.Add ( startingCanvas );
        //return;
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
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y + oneHalf ,    house.Rect.Width, otherHalf)));
            }
            else if (CanDivideHorizontally(house, MinimumHousePerimeter))
            {
                int oneHalf = house.Rect.Width - _random.Next(MinimumHousePerimeter, house.Rect.Width - MinimumHousePerimeter);
                int otherHalf = house.Rect.Width - oneHalf;
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y,             oneHalf , house.Rect.Height)));
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X + oneHalf, house.Rect.Y,   otherHalf, house.Rect.Height)));

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
        var containerInst = GameObject.Instantiate(container);
        containerInst.localPosition = container.localPosition;
        foreach (var item in _houses)
        {
            var inst = GameObject.Instantiate(prefab, containerInst);
            inst.localPosition = new Vector3(item.Rect.X / 3f, 0, item.Rect.Y / 3f);
            inst.localScale = new Vector3(item.Rect.Width /3f, .1f, item.Rect.Height /3f);
        }
        
    }
}
