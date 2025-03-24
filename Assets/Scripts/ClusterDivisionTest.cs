using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClusterDivisionTest : MonoBehaviour
{
    [SerializeField] Transform _housesContainer;
    [SerializeField] Transform _clusterContainer;
    [SerializeField] GameObject _housePrefab;
    [SerializeField] Color _minMaxClr = Color.red;
    [SerializeField] bool _showIndividualClusters;
    [SerializeField] bool _drawCells;
    int _currentDebuggedCluster;

    [ContextMenu("CleanupLonelyClusters")]
    public void CleanupLonelyClusters()
    {
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.GenerateHouses();
            cluster.Draw(_housePrefab.transform, _clusterContainer, _housesContainer);
            //Debug.Log(cluster.MinMax);

        }
    }

    [ContextMenu("GenerateHouses")]
    public void GenerateHouses()
    {
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.GenerateHouses();
            //Debug.Log(cluster.MinMax);

        }
    }
    
    [ContextMenu("DrawHouses")]
    public void DrawHouses()
    {
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.Draw(_housePrefab.transform, _clusterContainer, _housesContainer);
            //Debug.Log(cluster.MinMax);

        }
    }



    [ContextMenu("ClusterData")]
    public void ShowClusterData()
    {
        Debug.Log("Clusters Count: " + BuildSpace._merger.Count);
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            Debug.Log("Cluster id: " + cluster.ID);

            Debug.Log(cluster.MinMax);

        }
    }

    [ContextMenu("Random Cluster")]
    public void ShowRandomCluster()
    {
        var cluster = BuildSpace._merger.Values.ToList().GetRandomItem();
        Debug.Log("Spawning Cluster with id: " + cluster.ID);
        cluster.GenerateHouses();
        cluster.Draw(_housePrefab.transform, _clusterContainer, _housesContainer);
    }
    
    void OnDrawGizmos()
    {
        var clusters = BuildSpace._merger.Values;
        
        int i = 0;
        Gizmos.color = _minMaxClr;
        foreach (Cluster cluster in clusters)
        {
            if(_showIndividualClusters)
            {
                if (i == _currentDebuggedCluster)
                    cluster.DrawMinMax();
                i++;

            }
            else
            {
                cluster.DrawMinMax();
            }
            if(_drawCells) 
                cluster.DrawCells();

        }
        Gizmos.color = Color.white;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var clusters = BuildSpace._merger.Values;

            _currentDebuggedCluster++;
            if (_currentDebuggedCluster >= clusters.Count) _currentDebuggedCluster = 0;
        }
    }
}
