using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterDivisionTest : MonoBehaviour
{
    [SerializeField] Transform _housesContainer;
    [SerializeField] GameObject _housePrefab;
    [SerializeField] bool _do;

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

}
