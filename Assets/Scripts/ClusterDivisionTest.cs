using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterDivisionTest : MonoBehaviour
{
    [SerializeField] Transform _housesContainer;
    [SerializeField] GameObject _housePrefab;
    [SerializeField] bool _do;
    [SerializeField] Color _minMaxClr = Color.red;

    void Update()
    {
        if(_do)
        {
            _do = false;
            foreach (Cluster cluster in BuildSpace._merger.Values)
            {
                cluster.GenerateHouses();
                cluster.Draw(_housePrefab.transform, _housesContainer);
                //Debug.Log(cluster.MinMax);

            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _minMaxClr;
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.DrawMinMax();

        }
        Gizmos.color = Color.white;
    }
}
