using System;
using System.Collections.Generic;
using UnityEngine;
//make this generic
public class AStar
{
    readonly MySortedContainer<CellData> _priorityNodes = new MySortedContainer<CellData>();
    readonly Dictionary<Cell, CellData> _checkedNodes = new Dictionary<Cell, CellData>();
    
    class CellData : IComparable<CellData>
    {
        public bool Visited;
        public CellData Parent;
        public float DistanceToStart;
        public float Heuristics;
        public float Total => DistanceToStart + Heuristics;
        public readonly Cell Cell;

        public CellData(Cell cell, CellData parent, float distanceToStart, float heuristics)
        {
            Parent = parent;
            Cell = cell;
            DistanceToStart = distanceToStart;
            Heuristics = heuristics;
        }

        public int CompareTo(CellData other)
        {
            return Total.CompareTo(other.Total);
        }
    }

    public List<Cell> Find(Cell from, Cell goal)
    {
        var startNodeData = new CellData(from, null, 0, CalcWeight(from, goal));
        _priorityNodes.Enqueue(startNodeData);
        _checkedNodes.Add(from, startNodeData);
        CellData goalData = null;

        while (_priorityNodes.Count > 0)
        {
            CellData minNodeData = _priorityNodes.Dequeue();
            Cell minNode = minNodeData.Cell;

            if (minNode == goal)
            {
                goalData = minNodeData;
                break;
            }
            _checkedNodes[minNode].Visited = true;

            foreach (Tile connection in minNode.CollapsedTile.Neighbours)
            {
                Cell connectionCell = connection.ParentCell;
                float distanceToStart = CalcWeight(minNode, connectionCell) + minNodeData.DistanceToStart;
                float heuristics = CalcWeight(connectionCell, goal);

                if (!_checkedNodes.ContainsKey(connectionCell))
                {
                    CellData checkedNode = new CellData(connectionCell, minNodeData, distanceToStart, heuristics);
                    _priorityNodes.Enqueue(checkedNode);
                    _checkedNodes.Add(connectionCell, checkedNode);
                    continue;
                }
                CellData oldData = _checkedNodes[connectionCell];

                if (oldData.Visited) continue;

                bool pathIsNotCloser = oldData.Total <= distanceToStart + heuristics;
                if (pathIsNotCloser) continue;

                UpdatePathData(oldData, minNodeData, distanceToStart, heuristics);
            }
        }

        _checkedNodes.Clear();
        _priorityNodes.Clear();

        return goalData == null ? null : GetPath(goalData);
    }

    void UpdatePathData(CellData updatedNode, CellData previousNode, float distanceToStart, float heuristics)
    {
        updatedNode.Parent = previousNode;
        updatedNode.DistanceToStart = distanceToStart;
        updatedNode.Heuristics = heuristics;
    }

    void PopulateWithPath(List<Cell> path, CellData n)
    {
        if (n.Parent != null)
            PopulateWithPath(path, n.Parent);
        path.Add(n.Cell);
    }

    List<Cell> GetPath(CellData n)
    {
        var list = new List<Cell>();
        PopulateWithPath(list, n);
        return list;
    }

    float CalcWeight(Cell a, Cell b)
    {
        Vector2 aPos = new(a.X, a.Y);
        Vector2 bPos = new(b.X, b.Y);
        return aPos.GetManhattanDistance(bPos);
    }
}
