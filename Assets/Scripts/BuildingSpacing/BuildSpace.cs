using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

[Serializable]
public struct BuildCell
{
    public Vector2Int Position;
    public Collider Collider;
    public BuildSpace Cell;

    public BuildCell(Vector2Int position, Collider collider, BuildSpace cell)
    {
        Position = position;
        Collider = collider;
        Cell = cell;
    }
}

[Serializable]
public struct MinMax
{
    public int MinX;
    public int MinY;
    public int MaxX;
    public int MaxY;

    public MinMax(bool cheat,
        int minX = int.MaxValue,
        int minY = int.MaxValue,
        int maxX = int.MinValue,
        int maxY = int.MinValue)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public override string ToString()
    {
        return $"min: {MinX}, {MinY}\nmax:{MaxX}, {MaxY}";

    }
}

public class House
{
    public Rectangle Rect;

    public House(Rectangle area)
    {
        Rect = area;
    }
}

public class BuildSpace : MonoBehaviour
{

    public readonly static Dictionary<int, Cluster> _merger = new();
    static event Action<int, int> Merged;

    [SerializeField] List<BuildSpace> _links;

    [SerializeField] int _clusterID;

    readonly static System.Random Random = new System.Random(1);

    [SerializeField] GameObject _gameObject;
    [SerializeField] bool _update;
    //[SerializeField] MinMax _min;

    bool _isFirst = true;


    void Awake()
    {
        if(_isFirst)
        {
            _clusterID = GetInstanceID();
            _merger.Add(_clusterID, new Cluster(2, Random, _clusterID));
            foreach (BuildSpace link in _links)
            {
                link._isFirst = false;
                link._clusterID = _clusterID;
            }
            _isFirst = false;
        }
    }

    void OnValidate()
    {
        if (_update)
        {
            _update = false;
            //_min = _merger[_clusterID].MinMax;
            //debugCells = _merger[_clusterID].Values.Select(k => k.Collider).ToList();

        }
    }

    void OnEnable()
    {
        Merged += OnMerge;
    }


    void OnDisable()
    {
        Merged -= OnMerge;

    }

    void OnTriggerEnter(Collider other)
    {
        var otherID = other.GetInstanceID();
        
        bool isInCluster = _merger[_clusterID].ContainsKey(otherID);

        if (isInCluster) return;

        var otherBuildSpace = other.GetComponentInParent<BuildSpace>();
        //for debugging
        var obj = Instantiate(_gameObject, other.transform);
        obj.transform.localPosition = (other as BoxCollider).center;

        if(otherBuildSpace._clusterID != _clusterID)
        {
            Merge(this, otherBuildSpace);   
        }

        bool isInSyncedCluster = _merger[_clusterID].ContainsKey(otherID);

        if (isInSyncedCluster) return;

        Vector3 worldPos = obj.transform.position;
        Vector2Int gridPos = new(
            Mathf.FloorToInt(worldPos.x * 3),
            Mathf.FloorToInt(worldPos.z * 3)
        );

        //Debug.Log($"WorldPos: {worldPos}\nGridPos: {gridPos}");
        BuildCell otherData = new(gridPos, other, otherBuildSpace);
        Cluster syncedCluster = _merger[_clusterID];
        syncedCluster.Add(otherID, otherData);

        syncedCluster.UpdateMinMax(gridPos);
        //update cointaining box

        //debugCells = _merger[_clusterID].Values.Select(k => k.Collider).ToList();
    }

    static void Merge(BuildSpace a, BuildSpace b)
    {
        Cluster clusterA = _merger[a._clusterID];
        Cluster clusterB = _merger[b._clusterID];

        foreach (var bData in clusterB.Cells)
        {
            if (clusterA.ContainsKey(bData.Key)) continue;

            clusterA.Add(bData.Key, bData.Value);
        }
        clusterA.UpdateMinMax(clusterB.MinMax);
        _merger.Remove(b._clusterID);
        Merged?.Invoke(b._clusterID, a._clusterID);
        //point to an array and change the array 
    }


    void OnMerge(int oldClusterID, int newClusterID)
    {
        if(_clusterID != oldClusterID) return;

        _clusterID = newClusterID;
    }
    //add callback so every object that has the removed clusterID changes it to the persistent clusterID
}
