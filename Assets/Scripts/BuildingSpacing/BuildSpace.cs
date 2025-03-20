using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

    public readonly int Width => MaxX - MinX;
    public readonly int Height => MaxY - MinY;

    public override readonly string ToString()
    {
        return $"min: {MinX}, {MinY}\nmax:{MaxX}, {MaxY}\nW: {MaxX - MinX}, H: {MaxY - MinY}";

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
[SelectionBase]
public class BuildSpace : MonoBehaviour
{

    public readonly static Dictionary<int, Cluster> _merger = new();
    static event Action<int, int> Merged;

    [SerializeField] List<BuildSpace> _links;

    [SerializeField] int _clusterID;

    readonly static System.Random Random = new System.Random(1);

    [SerializeField] GameObject _gameObject;
    [SerializeField] bool _update;
    [SerializeField] MinMax _individualMinMax;
    [SerializeField] bool _showGizmos;
    [SerializeField] UnityEngine.Color color = UnityEngine.Color.red;
    bool _isFirst = true;

    void Awake()
    {
        
        if (_isFirst)
        {
            _clusterID = GetInstanceID();

            
            Cluster startCluster = new Cluster(1, Random, _clusterID);
            _merger.Add(_clusterID, startCluster );
            startCluster.UpdateMinMax(GetGridPosition(transform));

            foreach (BuildSpace link in _links)
            {
                link._isFirst = false;
                link._clusterID = _clusterID;

                startCluster.UpdateMinMax(GetGridPosition(link.transform));
                
            }
            _individualMinMax = startCluster.MinMax;

            _isFirst = false;
            
        }
    }
    void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        Gizmos.color = color;
        Vector3 dir = (transform.position - transform.parent.position).normalized * .3f;
        Gizmos.DrawRay(transform.parent.position, dir);
        Gizmos.DrawSphere(transform.parent.position+ dir, .1f);
    }

    void OnEnable()
    {
        Merged += OnMerge;
    }


    void OnDisable()
    {
        Merged -= OnMerge;

    }

    [ContextMenu("DebugClusterMinMax")]
    public void DebugClusterMinMax()
    {
        Debug.Log($"Cluster: {_clusterID} has :\n{GetCurrentCluster().MinMax}");
    }
    
    [ContextMenu("DebugIndividualMinMax")]
    public void DebugIndividualMinMax()
    {
        Debug.Log($"{_individualMinMax}");
    }

    Cluster GetCurrentCluster()
    {
        return _merger[_clusterID];
    }

    Vector2Int GetGridPosition(Transform target)
    {
        Vector3 dir = (target.position - target.parent.position).normalized * .33f;

        Vector3 worldPos = dir + target.parent.position;

        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x * 3),
            Mathf.FloorToInt(worldPos.z * 3)
        );
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
        Vector2Int gridPos = GetGridPosition(transform);
        
        //Debug.Log($"WorldPos: {worldPos}\nGridPos: {gridPos}");
        BuildCell otherData = new(gridPos, other, otherBuildSpace);
        Cluster syncedCluster = _merger[_clusterID];
        syncedCluster.Add(otherID, otherData);

        syncedCluster.UpdateMinMax(gridPos);

        //if (syncedCluster.MinMax.MaxY - syncedCluster.MinMax.MinY >= 3)
        //{
        //    transform.parent.position += Vector3.up;
        //    Debug.Log("aaa");
        //}
        //update cointaining box

        //debugCells = _merger[_clusterID].Values.Select(k => k.Collider).ToList();
    }

    void Merge(BuildSpace a, BuildSpace b)
    {
        Cluster clusterA = _merger[a._clusterID];
        Cluster clusterB = _merger[b._clusterID];

        foreach (var bData in clusterB.Cells)
        {
            if (clusterA.ContainsKey(bData.Key)) continue;

            clusterA.Add(bData.Key, bData.Value);
        }
        //var test = clusterA.fake(clusterB.MinMax);

        //if (test.MaxY - test.MinY >= 3)
        //{
        //}
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
