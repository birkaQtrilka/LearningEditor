using System;
using UnityEngine;
using UnityEngine.Events;

public class WorldSpawner : MonoBehaviour
{
    [SerializeField] float _size = 100f;
    [SerializeField] WorldGenConfig _config;
    [SerializeField] Transform _tileHolder;

    [SerializeField] bool _generateNewMap;

    public UnityEvent<Tile> TileCollapsed;
    public UnityEvent MapGenerated;

    void Start()
    {
        SpawnMap();   
    }

    private void Update()
    {
        if(_generateNewMap)
        {
            _generateNewMap = false;
            GameObject obj = new GameObject(_tileHolder.name);
            Destroy(_tileHolder.gameObject);
            _tileHolder = obj.transform;

            SpawnMap();
        }
    }

    void SpawnMap()
    {
        float cellWidth = _size / _config.Grid.GetHorizontalLength();

        foreach (GridCell cell in _config.Grid)
        {
            if (cell.tile.Prefab == null) continue;
            var inst = Instantiate(cell.tile.Prefab,
                transform.position + new Vector3(cell.X * cellWidth + .5f * cellWidth, 0, -cell.Y * cellWidth - .5f * cellWidth),
                Quaternion.Euler(90, cell.tile.Rotation, 0), _tileHolder);
            inst.transform.localScale = Vector3.one * (cellWidth);

        }
    }
}
