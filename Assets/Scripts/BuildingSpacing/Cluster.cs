﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Cluster
{
    public Dictionary<int, BuildCell> Cells { get; private set; } = new();
    public MinMax MinMax;
    public int MinimumHousePerimeter;

    readonly Queue<House> _toDoHouses = new();
    readonly List<House> _houses = new();

    readonly System.Random _random;
    readonly int _minHouses;
    readonly int _maxArea;
    readonly int _maxSideDifference;
    readonly int _splitChance;

    public readonly int ID;
    UnityEngine.Color _debugClr;

    public Cluster(int minimumHousePerimeter, System.Random random, int id,  int minHouses = 2, int maxArea = 10, int maxSideDifference = 2, int splitChance = 3)
    {
        MinimumHousePerimeter = minimumHousePerimeter;
        _random = random;
        _minHouses = minHouses;
        _maxArea = maxArea;
        _maxSideDifference = maxSideDifference;
        _splitChance = splitChance;
        ID = id;
        MinMax = new MinMax(true);
        _debugClr  = UnityEngine.Random.ColorHSV(0f,1f,1f,1f,1f,1f);
    }

    public bool ContainsKey(int k)
    {
        return Cells.ContainsKey(k);
    }

    public void Add(int k, BuildCell cell)
    {
        Cells.Add(k, cell);
    }
    //go through every cell, 
    public void Remove(int k)
    {
        Cells.Remove(k);
    }

    //public MinMax fake(MinMax minMax)
    //{
    //    var copy = MinMax;

    //    if (minMax.MinX < MinMax.MinX) copy.MinX = minMax.MinX;
    //    if (minMax.MaxX > MinMax.MaxX) copy.MaxX = minMax.MaxX;

    //    if (minMax.MinY < MinMax.MinY) copy.MinY = minMax.MinY;
    //    if (minMax.MaxY > MinMax.MaxY) copy.MaxY = minMax.MaxY;
    //    return copy;
    //}

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
                width: MinMax.MaxX - MinMax.MinX + 1,
                height: MinMax.MaxY - MinMax.MinY + 1 
            )
        );
        _toDoHouses.Enqueue( startingCanvas );
        while (_toDoHouses.Count > 0)
        {
            House house = _toDoHouses.Dequeue();
            //if everything is true, 
            bool requirementsBeforeStoppingDivision =
                splits > _minHouses &&
                house.Rect.Width * house.Rect.Height < _maxArea &&  
                Mathf.Abs(house.Rect.Width - house.Rect.Height) <= _maxSideDifference;
                ;
                
            if ( requirementsBeforeStoppingDivision && _random.Next(0, _splitChance) == 1)
            {
                AddHouse( house );
                continue;
            }

            if (house.Rect.Width < house.Rect.Height && CanDivideVertically(house, MinimumHousePerimeter))
            {
                int oneHalf = house.Rect.Height - _random.Next(MinimumHousePerimeter, house.Rect.Height - MinimumHousePerimeter);
                int otherHalf = house.Rect.Height - oneHalf;

                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y, house.Rect.Width, oneHalf)));
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y + oneHalf, house.Rect.Width, otherHalf)));
            }
            else if (CanDivideHorizontally(house, MinimumHousePerimeter))
            {
                int oneHalf = house.Rect.Width - _random.Next(MinimumHousePerimeter, house.Rect.Width - MinimumHousePerimeter);
                int otherHalf = house.Rect.Width - oneHalf;
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X, house.Rect.Y, oneHalf, house.Rect.Height)));
                _toDoHouses.Enqueue(new House(new Rectangle(house.Rect.X + oneHalf, house.Rect.Y, otherHalf, house.Rect.Height)));

            }
            else
                AddHouse(house);

            splits += 2;
        }
    }

    void AddHouse(House house)
    {
        _houses.Add(house);
        house.ClusterCells.AddRange(Cells.Values.Where(c => Contains(house.Rect, c.Position)));

    }

    bool Contains(Rectangle r, Vector2Int p)
    {
        return p.x >= r.X && p.x <= r.X + r.Width &&
               p.y >= r.Y && p.y <= r.Y + r.Height ;
    }

    bool CanDivideVertically(House house, int pMinimumRoomSize)
    {
        return house.Rect.Height - pMinimumRoomSize >= pMinimumRoomSize;
    }

    bool CanDivideHorizontally(House house, int pMinimumRoomSize)
    {
        return house.Rect.Width - pMinimumRoomSize >= pMinimumRoomSize;
    }

    public void Draw(Transform prefab, Transform clusterContainer, Transform housesContainer)
    {
        var containerInst = GameObject.Instantiate(clusterContainer, housesContainer);
        containerInst.localPosition = clusterContainer.localPosition;
        foreach (House item in _houses)
        {
            var inst = GameObject.Instantiate(prefab, containerInst);
            inst.localScale = new Vector3(item.Rect.Width /3f , .1f, item.Rect.Height /3f);

            var corner_TL = new Vector3(item.Rect.X, 1, item.Rect.Y);
            //magic number 3 is the  scale of the cells
            corner_TL /= 3f;
            inst.transform.position = corner_TL;
        }
        
    }
    readonly Vector3[] arr = new Vector3[8];

    public void DrawCells()
    {
        foreach (House house in _houses)
        {
            foreach (BuildCell cell in house.ClusterCells)
            {
                float x = cell.Position.x;
                float y = cell.Position.y;
                float x1 = (cell.Position.x + 1);
                float y1 = (cell.Position.y + 1);

                var corner_TL = new Vector3(x, 1, y)      * .3333f;
                var corner_TR = new Vector3(x1, 1, y)     * .3333f;
                var corner_BR = new Vector3(x1, 1, y1)    * .3333f;
                var corner_BL = new Vector3(x, 1, y1)     * .3333f;
                arr[0] = corner_TL;
                arr[1] = corner_TR;

                arr[2] = corner_TR;
                arr[3] = corner_BR;

                arr[4] = corner_BR;
                arr[5] = corner_BL;

                arr[6] = corner_BL;
                arr[7] = corner_TL;


                Gizmos.DrawLineList(arr);
               
            }
        }
    }

    public void DrawMinMax()
    {
        //if (Cells.Count == 0)
        //{
        //    return;
        //}
        var corner_TL = new Vector3(MinMax.MinX,     1, MinMax.MinY);
        var corner_TR = new Vector3(MinMax.MaxX + 1, 1, MinMax.MinY);
        var corner_BR = new Vector3(MinMax.MaxX + 1, 1, MinMax.MaxY + 1);
        var corner_BL = new Vector3(MinMax.MinX,     1, MinMax.MaxY + 1);

        corner_TL /= 3f;
        corner_TR /= 3f;
        corner_BR /= 3f;
        corner_BL /= 3f;

        //Gizmos.color = _debugClr;
        Handles.color = _debugClr;
        Handles.DrawLine(corner_TL, corner_TR, 3);
        Handles.DrawLine(corner_TR, corner_BR, 3);
        Handles.DrawLine(corner_BR, corner_BL, 3);
        Handles.DrawLine(corner_BL, corner_TL, 3);
    }
}
