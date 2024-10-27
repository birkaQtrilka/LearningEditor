using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenConfig))]
public class WorldGenConfigDrawer : Editor
{
    WorldGenConfig SO;
    
    void OnEnable()
    {
        
    }

    public override void OnInspectorGUI()
    {
        SO = (WorldGenConfig)target;
        //serializedObject.Update();
        if (SO.Grid == null || SO.Grid.GetHorizontalLength() != SO.Columns)
        {
            EditorApplication.delayCall += () =>
            {
                if (SO == null) return;

                Undo.RecordObject(SO, "Resetting grid");

                SO.Grid = new GridCellCollection(SO.Columns);

                SaveObject();
            };
            return;
        }

        DrawSeedField();
        DrawGridColumnsField();
        DrawSocketCountField();
        DrawAvailableTiles();
        if (GUILayout.Button("GenerateGridRandom"))
        {
            SO.GenerateGrid(Random.Range(0,1000));
            SaveObject();
            return;
        }
        if (GUILayout.Button("GenerateGrid"))
        {
            SO.GenerateGrid();
            SaveObject();
            return;
        }

        if (GUILayout.Button("DestroyGrid"))
        {
            SO.DestroyMap();
            SaveObject();
            return;
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawGrid"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawSockets"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawNames"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowHollowTiles"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SocketColors"));

        if(SO.drawGrid)
            DrawGrid();

        
        serializedObject.ApplyModifiedProperties();
    }
    

    void SaveObject()
    {
        Debug.Log("Saving");
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void DrawGrid()
    {
        SerializedProperty gridProp = serializedObject.FindProperty("Grid");
        SerializedProperty gridRowsProp = gridProp.FindPropertyRelative("_cellRows");

        var tileNames = SO.AvailableTiles.Where(t => t.Prefab != null).Select(t => t.Prefab.name).Prepend("None").ToArray();
        float screenWidth = EditorGUIUtility.currentViewWidth;
        float cellSize = (screenWidth - EditorStyles.inspectorDefaultMargins.margin.horizontal - (GUI.skin.button.margin.horizontal * SO.Columns)) / SO.Columns;
        Color defaultColor = GUI.backgroundColor;
        
        GUILayout.BeginVertical();
        for (int y = 0; y < SO.Columns; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < SO.Columns; x++)
            {
                if (SO.Grid[y,x] == null || 
                    SO.Grid[y, x].tile == null || 
                    (SO.Grid[y, x].tile != null && IsEmpty(SO.Grid[y,x].tile) && SO.Grid[y, x].PopUpIndex != 0))//remove ghost tiles that remained after errors
                {
                    SetCellProp(x, y, new() { tile = new Tile()}, gridProp, gridRowsProp);
                }

                ColorTiles(x, y);
                bool buttonIsPressed = GUILayout.Button(new GUIContent(SO.drawNames ? tileNames[SO.Grid[y, x].PopUpIndex] : ""), GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                if(SO.drawSockets)
                    DrawSocketsRects(GUILayoutUtility.GetLastRect(), SO.Grid[y, x].tile.Sockets);

                if (!buttonIsPressed) continue;

                if (IsMiddleClick())
                {
                    Debug.Log(SO.Grid[y, x]);

                    continue;
                }

                if (IsRightClick())
                    SO.Grid[y, x].tile?.Rotate();//you should set the serialized property
                    //Debug.Log(SO.Grid[y, x].tile?.Sockets);
                else
                    DrawMenu(x, y, tileNames);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        
        GUI.backgroundColor = defaultColor;

    }

    void DrawSocketsLetters(Rect container, Sockets sockets)
    {
        if (sockets == null) return;
        GUIStyle style = new()
        {
            alignment = TextAnchor.MiddleCenter,
        };
        //up
        EditorGUI.LabelField
        (
            new Rect(container.position, new Vector2(container.width, EditorGUIUtility.singleLineHeight)),
            sockets.GetSocket(NeighbourDir.Up),
            style
        );
        
        //right
        EditorGUIUtility.RotateAroundPivot(90f, container.center);
        EditorGUI.LabelField
        (
            new Rect(container.position, new Vector2(container.width, EditorGUIUtility.singleLineHeight)),
            sockets.GetSocket(NeighbourDir.Right),
            style
        );

        //down
        EditorGUIUtility.RotateAroundPivot(90f, container.center);
        EditorGUI.LabelField
        (
            new Rect(container.position, new Vector2(container.width, EditorGUIUtility.singleLineHeight)),
            sockets.GetSocket(NeighbourDir.Down),
            style
        );

        //left
        EditorGUIUtility.RotateAroundPivot(90f, container.center);
        EditorGUI.LabelField
        (
            new Rect(container.position, new Vector2(container.width, EditorGUIUtility.singleLineHeight)),
            sockets.GetSocket(NeighbourDir.Left),
            style
        );
        EditorGUIUtility.RotateAroundPivot(-270f, container.center);
    }

    void DrawSocketsRects(Rect container, Sockets sockets)
    {
        if (sockets == null || sockets.GetSocket(0) == null) return;

        float rectWidth = container.width / SO.SocketsCount;
        foreach (string edge in sockets)
        {
            for (int i = 0; i < edge.Length; i++)
            {
                EditorGUI.DrawRect(new Rect(container.position + i * rectWidth * Vector2.right, new(rectWidth, 5)), GetColor(edge[i]));
            }
            
            EditorGUIUtility.RotateAroundPivot(90f, container.center);
        }

    }

    Color GetColor(char socket)
    {
        return SO.SocketColors.FirstOrDefault(sc=> sc.socket == socket).color;
    }

    void SetCellProp(int x, int y, GridCell cell, SerializedProperty cachedGrid = null, SerializedProperty cachedRows = null)
    {
        cachedGrid ??= serializedObject.FindProperty("Grid");
        cachedRows ??= cachedGrid.FindPropertyRelative("_cellRows");

        GetCellProp(cachedRows, x, y).SetValueOnScriptableObject(cell);
    }

    SerializedProperty GetCellProp(SerializedProperty gridRows, int x, int y)
    {
        return gridRows
            .GetArrayElementAtIndex(y)
            .FindPropertyRelative("Cells")
            .GetArrayElementAtIndex(x);
    }

    void DrawMenu(int x, int y, string[] tileNames)
    {
        GenericMenu menu = new();
        for (int i = 0; i < tileNames.Length; i++) AddMenuItem(menu, tileNames[i], i, x, y);
        menu.ShowAsContext();
    }    

    // a method to simplify adding menu items
    void AddMenuItem(GenericMenu menu, string menuPath, int popUpIndex, int x, int y)
    {
        // the menu item is marked as selected if it matches the current popUpIndex
        menu.AddItem(new GUIContent(menuPath), SO.Grid[y,x].PopUpIndex == popUpIndex, OnMenuButtonPress, new MenuData(x, y, popUpIndex));
    }

    void OnMenuButtonPress(object menuDataObj)
    {
        MenuData data = (MenuData)menuDataObj;
        GridCell newCell = new() 
        {
            PopUpIndex = data.PopUpIndex, 
            tile = data.PopUpIndex == 0 ? null : SO.AvailableTiles[data.PopUpIndex - 1].Clone() 
        };
        SetCellProp(data.X,data.Y,newCell);
        SaveObject();

    }

    void ColorTiles(int x, int y)
    {
        Tile tile = SO.Grid[y, x].tile;
        //showing if selected tiles have valid connections
        if (!IsEmpty(tile) && !AllValidConnections(x, y, tile, SO.Grid))
            GUI.backgroundColor = Color.red;
        else if (!IsEmpty(tile))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = new Color(0.76f, 0.76f, 0.76f); ;//default color
    }

    void DrawSeedField()
    {
        SerializedProperty seedProp = serializedObject.FindProperty("_seed");
        seedProp.intValue = Mathf.Clamp(seedProp.intValue, 1, 20);
        EditorGUILayout.PropertyField(seedProp);
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

    void DrawAvailableTiles()
    {
        SerializedProperty availableTiles = serializedObject.FindProperty("AvailableTiles");
        EditorGUILayout.PropertyField(availableTiles,true);
        //LimitSocketLength(availableTiles);
    }

    bool IsRightClick()
    {
        return Event.current.button == 1;
    }

    bool IsMiddleClick()
    {
        return Event.current.button == 2;
    }

    bool AllValidConnections(int x, int y, Tile currentTile, GridCellCollection grid)
    {
        bool IsNotConnected(Tile otherTile, NeighbourDir dir)
        {
            return !IsEmpty(otherTile) && !currentTile.CanConnect(otherTile, dir);
        }

        //check if I'm bounds and then if  the tile is not connected
        if 
        (
            (x - 1 >= 0                         && IsNotConnected(grid[y, x - 1].tile, NeighbourDir.Left))
            ||
            (x + 1 < grid.GetHorizontalLength() && IsNotConnected(grid[y, x + 1].tile, NeighbourDir.Right))
            ||
            (y - 1 >= 0                         && IsNotConnected(grid[y - 1, x].tile, NeighbourDir.Up))
            ||
            (y + 1 < grid.GetVerticalLength()   && IsNotConnected(grid[y + 1, x].tile, NeighbourDir.Down))
        )
            return false;

        return true;
    }

    bool IsEmpty(Tile tile)
    {
        return tile.Prefab == null;
    }

    ///// <summary>
    ///// Sets the string from the inspector to be length of allowed socket length (if string is bigger than allowed)
    ///// </summary>
    ///// <param name="tilesListProperty"></param>
    //void LimitSocketLength(SerializedProperty tilesListProperty)
    //{
    //    for (int i = 0; i < tilesListProperty.arraySize; i++)
    //    {
    //        SerializedProperty edges = tilesListProperty
    //            .GetArrayElementAtIndex(i)
    //            .FindPropertyRelative("_sockets")
    //            .FindPropertyRelative("_edges");
    //        for (int j = 0; j < edges.arraySize; j++)
    //        {
    //            SerializedProperty edge = edges.GetArrayElementAtIndex(j);
    //            edge.stringValue = LimitLength(SO.SocketsCount, edge.stringValue);
    //        }
    //    }
    //}

    //string LimitLength(int length, string text)
    //{
    //    if (text.Length > length)
    //        return text[..length];
    //    return text;
    //}

    readonly struct MenuData
    {
        //public readonly SerializedProperty GridCellProp;
        public readonly int X;
        public readonly int Y;
        public readonly int PopUpIndex;

        public MenuData(/*SerializedProperty cellProp,*/int x, int y, int popUpIndex)
        {
            X = x; 
            Y = y;
            //GridCellProp = cellProp;
            PopUpIndex = popUpIndex;
        }
    }
}
