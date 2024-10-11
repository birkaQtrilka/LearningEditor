using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(WorldGenConfig))]
public class WorldGenConfigDrawer : Editor
{
    WorldGenConfig monoBehavior;
    
    public string LimitLength(int length, string text)
    {
        if (text.Length > length)
            return text[..length];
        return text;
    }

    public override void OnInspectorGUI()
    {
        monoBehavior = (WorldGenConfig)target;

        DrawGridColumnsField();
        DrawSocketCountField();
        SerializedProperty list = serializedObject.FindProperty("AvailableTiles");
        EditorGUILayout.PropertyField(list, true);
        LimitSocketLength(list);
        SerializedProperty gridProp = serializedObject.FindProperty("Grid");

        if (monoBehavior.Grid == null || monoBehavior.Grid.GetHorizontalLength() != monoBehavior.Columns)
        {
            gridProp.SetValueOnScriptableObject(new GridCellCollection(monoBehavior.Columns));
            Debug.Log("reset");
        }

        DrawGrid(gridProp);

        serializedObject.ApplyModifiedProperties();
    }
    
    void DrawGrid(SerializedProperty gridProp)
    {
        serializedObject.Update();
        float screenWidth = EditorGUIUtility.currentViewWidth;
        SerializedProperty gridRows = gridProp.FindPropertyRelative("_cellRows");
        
        var tileNames = monoBehavior.AvailableTiles.Where(t => t.Prefab != null).Select(t => t.Prefab.name).Prepend("None").ToArray();
        float cellSize = (screenWidth - EditorStyles.inspectorDefaultMargins.margin.horizontal - (GUI.skin.button.margin.horizontal * monoBehavior.Columns)) / monoBehavior.Columns;
        Color defaultColor = GUI.backgroundColor;

        GUILayout.BeginVertical();

        for (int y = 0; y < monoBehavior.Columns; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < monoBehavior.Columns; x++)
            {
                SerializedProperty currentCellProp = GetCellProp(gridRows, x, y);
                Tile currentTile = monoBehavior.Grid[y,x].tile;
                if (currentTile != null && currentTile.Prefab == null)//remove ghost tiles that remained after errors
                {
                    ((GridCell)currentCellProp.objectReferenceValue).tile = null;//is this allowed?

                    currentTile = null;
                }

                //showing if selected tiles have valid connections
                if (currentTile != null && !AllValidConnections(x, y, currentTile, monoBehavior.Grid))
                    GUI.backgroundColor = Color.red;
                else if (currentTile != null)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = defaultColor;

                if (!GUILayout.Button(new GUIContent(tileNames[monoBehavior.Grid[y, x].PopUpIndex]), GUILayout.Width(cellSize), GUILayout.Height(cellSize))) continue;
                
                if (IsRightClick())
                {
                    currentTile?.Rotate();
                    Debug.Log(currentTile?.Sockets);
                    continue;
                }
                GenericMenu menu = new();
                for (int i = 0; i < tileNames.Length; i++) AddMenuItem(menu, tileNames[i], i, currentCellProp,x,y);
                menu.ShowAsContext();

            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUI.backgroundColor = defaultColor;
    }

    SerializedProperty GetCellProp(SerializedProperty gridRows, int x, int y)
    {
        SerializedProperty horizontalArrayProp = gridRows.GetArrayElementAtIndex(y).FindPropertyRelative("Cells");
        return horizontalArrayProp.GetArrayElementAtIndex(x);
    }

    // a method to simplify adding menu items
    void AddMenuItem(GenericMenu menu, string menuPath, int popUpIndex, SerializedProperty cellProp, int x, int y)
    {
        // the menu item is marked as selected if it matches the current popUpIndex
        menu.AddItem(new GUIContent(menuPath), monoBehavior.Grid[y,x].PopUpIndex == popUpIndex, OnMenuButtonPress, new MenuData(cellProp, popUpIndex));
    }

    void OnMenuButtonPress(object menuDataObj)
    {
        MenuData data = (MenuData)menuDataObj;
        GridCell newCell = new() 
        {
            PopUpIndex = data.PopUpIndex, 
            tile = data.PopUpIndex == 0 ? null : monoBehavior.AvailableTiles[data.PopUpIndex - 1].Clone() 
        };
        data.GridCellProp.objectReferenceValue = newCell;
    }

    void DrawGridColumnsField()
    {
        SerializedProperty columnsProp = serializedObject.FindProperty("Columns");
        columnsProp.intValue = Mathf.Clamp(columnsProp.intValue, 1, 20);
        EditorGUILayout.PropertyField(columnsProp);
    }

    void DrawSocketCountField()
    {
        SerializedProperty socketCountProp = serializedObject.FindProperty("SocketsCount");
        socketCountProp.intValue = Mathf.Clamp(socketCountProp.intValue, 1, 5);
        EditorGUILayout.PropertyField(socketCountProp);
    }

    bool IsRightClick()
    {
        return Event.current.button == 1;
    }

    bool AllValidConnections(int x, int y, Tile currentTile, GridCellCollection grid)
    {
        //check if I'm bounds and then if  the tile is not connected
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

    void LimitSocketLength(SerializedProperty tilesProperty)
    {
        for (int i = 0; i < tilesProperty.arraySize; i++)
        {
            var som = tilesProperty.GetArrayElementAtIndex(i);
            var som2 = som.FindPropertyRelative("_sockets");
            SerializedProperty edges = som2.FindPropertyRelative("_edges");
            for (int j = 0; j < edges.arraySize; j++)
            {
                SerializedProperty edge = edges.GetArrayElementAtIndex(j);
                edge.stringValue = LimitLength(monoBehavior.SocketsCount, edge.stringValue);
            }
        }
    }

    readonly struct MenuData
    {
        public readonly SerializedProperty GridCellProp;
        public readonly int PopUpIndex;

        public MenuData(SerializedProperty cellProp, int popUpIndex)
        {
            GridCellProp = cellProp;
            PopUpIndex = popUpIndex;
        }
    }

   
}
