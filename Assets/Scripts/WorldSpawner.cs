using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class WorldSpawner : MonoBehaviour
{
    [SerializeField] float _size = 100f;
    [SerializeField] WorldGenConfig _config;
    [SerializeField] Transform _tileHolder;

    [SerializeField] bool _generateNewMap;
    [SerializeField] bool _spawnOnStart;
    public UnityEvent<Tile> TileCollapsed;
    public UnityEvent MapGenerated;
    [SerializeField] GameObject _housePrefab;
    [SerializeField] bool _showHouses;
    [SerializeField] Transform _housesContainer;

    void Start()
    {
        if(_spawnOnStart)
            SpawnMap(false);   

    }

    void Update()
    {
        if(_generateNewMap)
        {
            _generateNewMap = false;
            GameObject obj = new GameObject(_tileHolder.name);
            Destroy(_tileHolder.gameObject);
            _tileHolder = obj.transform;

            SpawnMap(true);
        }

        if(_showHouses)
        {
            _showHouses = false;
            ShowHouses();
        }

        
    }

    void OnDrawGizmos()
    {
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.DrawMinMax();
        }
    }

    public void ShowHouses()
    {
        foreach (Cluster cluster in BuildSpace._merger.Values)
        {
            cluster.GenerateHouses();
            cluster.Draw(_housePrefab.transform, _housesContainer);

        }

    }

    [ContextMenu("Random Cluster")]
    public void ShowRandomCluster()
    {
        var cluster = BuildSpace._merger.Values.ToList().GetRandomItem();
        Debug.Log("Spawning Cluster with id: " + cluster.ID);
        cluster.GenerateHouses();
        cluster.Draw(_housePrefab.transform, _housesContainer);
    }

    void SpawnMap(bool random)
    {
        if (random)
        {
            _config.DestroyMap();
            _config.GenerateGrid(UnityEngine.Random.Range(0, 10000));

        }

        float cellWidth = _size / _config.Grid.GetHorizontalLength();

        foreach (GridCell cell in _config.Grid)
        {
            if (cell.tile.Prefab == null) continue;
            var inst = Instantiate(cell.tile.Prefab,
                transform.position + new Vector3(cell.X * cellWidth + .5f * cellWidth, 0, -cell.Y * cellWidth - .5f * cellWidth),
                Quaternion.Euler(90, cell.tile.Rotation, 0), _tileHolder);
            SpriteRenderer renderer = inst.GetComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one;
            inst.transform.localScale = new Vector3(cellWidth, cellWidth);
        }
    }

    [ContextMenu("SpawnMap")]
    public void SpawnConfigMap()
    {
        GameObject obj = new GameObject(_tileHolder.name);
        DestroyImmediate(_tileHolder.gameObject);
        _tileHolder = obj.transform;
        SpawnMap(false);
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


}
