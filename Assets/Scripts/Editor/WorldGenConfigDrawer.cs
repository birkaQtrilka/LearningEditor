using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static WorldGenConfig;

[CustomEditor(typeof(WorldGenConfig))]
public class WorldGenConfigDrawer : UnityEditor.Editor
{
    WorldGenConfig monoBehavior;
    
    void OnEnable()
    {
        monoBehavior = (WorldGenConfig)target;
    }

    public override void OnInspectorGUI()
    {
        //if (GUILayout.Button("reset grid"))
        //monoBehavior.Grid = new GridCell[monoBehavior.Columns, monoBehavior.Columns];
        float screenWidth = EditorGUIUtility.currentViewWidth;

        SerializedProperty list = serializedObject.FindProperty("AvailableTiles");

        int newColumnsValue = EditorGUILayout.IntField("Columns", monoBehavior.Columns);
        monoBehavior.Columns = Mathf.Clamp(newColumnsValue, 0, 10);
        int newSocketsCountValue = EditorGUILayout.IntField("SocketsCount", monoBehavior.SocketsCount);
        monoBehavior.SocketsCount = Mathf.Clamp(newSocketsCountValue, 1, 5);
        EditorGUILayout.PropertyField(list, true);


        var tileNames = monoBehavior.AvailableTiles.Where(t => t.Prefab != null).Select(t => t.Prefab.name).Prepend("None").ToArray();
        ////Vector2 padding = new Vector2(1, 1);
        float cellSize = (screenWidth - EditorStyles.inspectorDefaultMargins.margin.horizontal - (GUI.skin.button.margin.horizontal * monoBehavior.Columns)) / monoBehavior.Columns;
        Color defaultColor = GUI.backgroundColor;
        if (monoBehavior.Grid == null)
        {
            Debug.Log("grid is null");
        }else if(monoBehavior.Grid.GetVerticalLength() != monoBehavior.Columns)
        {
            Debug.Log("grid is not matching columns");

        }

        if (monoBehavior.Grid == null || monoBehavior.Grid.GetVerticalLength() != monoBehavior.Columns)
        {
            monoBehavior.Grid = new GridCellCollection(monoBehavior.Columns);
        }
        GUILayout.BeginVertical();
        GridCellCollection grid = monoBehavior.Grid;

        for (int y = 0; y < monoBehavior.Columns; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < monoBehavior.Columns; x++)
            {
                monoBehavior.Grid[y, x] ??= new GridCell();

                Tile currentTile = grid[y, x].tile;
                if(currentTile != null && currentTile.Prefab == null)
                {
                    grid[y, x].tile = null;
                }
                if (currentTile != null && !AllValidConnections(x,y,currentTile,grid))
                    GUI.backgroundColor = Color.red;
                else if(currentTile != null)
                    GUI.backgroundColor = Color.green;
                else 
                    GUI.backgroundColor = defaultColor;
                if (GUILayout.Button(new GUIContent(tileNames[monoBehavior.Grid[y, x].PopUpIndex]), GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    GenericMenu menu = new();
                    for (int i = 0; i < tileNames.Length; i++)
                    {
                        AddMenuItemForColor(menu, tileNames[i], i, x, y);
                    }
                    menu.ShowAsContext();
                }

            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        serializedObject.ApplyModifiedProperties();
        GUI.backgroundColor = defaultColor;
    }

    bool AllValidConnections(int x, int y, Tile currentTile, GridCellCollection grid)
    {
        
        if 
        (
            (x - 1 >= 0 && IsNotConnected(grid[y, x - 1].tile, NeighbourDir.Left))
            ||
            (x + 1 < grid.GetHorizontalLength() && IsNotConnected(grid[y, x + 1].tile, NeighbourDir.Right))
            ||
            (y - 1 >= 0 && IsNotConnected(grid[y - 1, x].tile, NeighbourDir.Up))
            ||
            (y + 1 < grid.GetVerticalLength() && IsNotConnected(grid[y + 1, x].tile, NeighbourDir.Down))
        )
            return false;

        return true;
        bool IsNotConnected(Tile otherTile, NeighbourDir dir)
        {
            return otherTile != null && !currentTile.CanConnect(otherTile, dir);
        }
    }

    // a method to simplify adding menu items
    void AddMenuItemForColor(GenericMenu menu, string menuPath, int popUpIndex, int x, int y)
    {
        // the menu item is marked as selected if it matches the current value of m_Color
        menu.AddItem(new GUIContent(menuPath), monoBehavior.Grid[y, x].PopUpIndex == popUpIndex, OnMenuButtonPress, new MenuData(x,y,popUpIndex));
    }

    readonly struct MenuData
    {
        public readonly int X;
        public readonly int Y;
        public readonly int PopUpIndex;

        public MenuData(int x, int y, int popUpIndex)
        {
            X = x;
            Y = y;
            PopUpIndex = popUpIndex;
        }
    }

    void OnMenuButtonPress(object menuDataObj)
    {
        MenuData data = (MenuData)menuDataObj;
        GridCell cell = monoBehavior.Grid[data.Y, data.X];
        cell.PopUpIndex = data.PopUpIndex;
        cell.tile = data.PopUpIndex == 0 ? null : monoBehavior.AvailableTiles[data.PopUpIndex - 1].Clone();
    }
}
