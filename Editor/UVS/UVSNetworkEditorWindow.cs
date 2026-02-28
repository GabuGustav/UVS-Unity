using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Mathematics;
using UVS.Editor.Network;

public class UVSNetworkEditorWindow : EditorWindow
{
    private int _tab;
    private Vector2 _scroll;
    private RoadNetworkAsset _road;
    private RailNetworkAsset _rail;
    private GridNetworkAsset _grid;

    private bool _showSceneDebug = true;
    private bool _showNodeIds = true;
    private bool _showEdges = true;
    private bool _editNodes;
    private float _nodeSize = 0.35f;
    private float _edgeWidth = 3f;
    private Color _roadNodeColor = new(1f, 0.75f, 0.1f, 0.95f);
    private Color _railNodeColor = new(0.2f, 0.85f, 1f, 0.95f);
    private Color _edgeColor = new(0.15f, 0.8f, 1f, 0.85f);

    private SplineContainer _edgeSplineOverride;
    private bool _autoCreateSplineIfMissing = true;
    private float _createdSplineLength = 30f;
    private bool _selectCreatedSpline = true;
    private string _edgeStatus = "Select a network and add edge splines from the current selection.";
    private MessageType _edgeStatusType = MessageType.Info;

    private bool _buildInBackground = true;

    // Road build settings.
    private GameObject _roadSegmentPrefab;
    private float _roadSpacing = 2f;
    private bool _roadAutoSpacingFromPrefab = true;
    private float _roadSpacingMultiplier = 1f;
    private bool _roadAlignToSpline = true;
    private bool _roadConformToTerrain = true;
    private LayerMask _roadTerrainMask = ~0;
    private float _roadRayStartHeight = 100f;
    private float _roadRayDistance = 500f;
    private float _roadTerrainOffset = 0f;
    private bool _roadAlignToTerrainNormal;

    // Rail build settings.
    private GameObject _railTrackPrefab;
    private GameObject _railSleeperPrefab;
    private float _railTrackSpacing = 2.5f;
    private float _railSleeperSpacing = 0.6f;
    private bool _railAutoTrackSpacing = true;
    private float _railTrackSpacingMultiplier = 1f;
    private bool _railAutoSleeperSpacing = true;
    private float _railSleeperSpacingMultiplier = 1f;
    private bool _railAlignToSpline = true;
    private bool _railConformToTerrain = true;
    private LayerMask _railTerrainMask = ~0;
    private float _railRayStartHeight = 100f;
    private float _railRayDistance = 500f;
    private float _railTerrainOffset = 0f;
    private bool _railAlignToTerrainNormal;

    [MenuItem("Tools/Vehicle Editor/Network Editor")]
    public static void ShowWindow()
    {
        GetWindow<UVSNetworkEditorWindow>("UVS Network Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (UVSNetworkBuildQueue.IsRunning)
            UVSNetworkBuildQueue.Cancel();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawSceneDebugControls();
        GUILayout.Space(6);

        _tab = GUILayout.Toolbar(_tab, new[] { "Road", "Rail", "Grid" });
        GUILayout.Space(8);

        switch (_tab)
        {
            case 0: DrawRoadTab(); break;
            case 1: DrawRailTab(); break;
            case 2: DrawGridTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRoadTab()
    {
        _road = (RoadNetworkAsset)EditorGUILayout.ObjectField("Road Network", _road, typeof(RoadNetworkAsset), false);
        if (_road == null && Selection.activeObject is RoadNetworkAsset selectedRoad)
            _road = selectedRoad;

        if (GUILayout.Button("Create Road Network Asset"))
            _road = CreateAsset<RoadNetworkAsset>("RoadNetwork.asset");

        DrawEdgeTools(_road);

        if (_road != null)
        {
            EditorGUILayout.LabelField($"Nodes: {_road.nodes.Count}  Edges: {_road.edges.Count}");
            if (GUILayout.Button("Frame Road Network"))
                FrameNetwork(_road);
        }

        DrawRoadBuildTools();
    }

    private void DrawRailTab()
    {
        _rail = (RailNetworkAsset)EditorGUILayout.ObjectField("Rail Network", _rail, typeof(RailNetworkAsset), false);
        if (_rail == null && Selection.activeObject is RailNetworkAsset selectedRail)
            _rail = selectedRail;

        if (GUILayout.Button("Create Rail Network Asset"))
            _rail = CreateAsset<RailNetworkAsset>("RailNetwork.asset");

        DrawEdgeTools(_rail);

        if (_rail != null)
        {
            EditorGUILayout.LabelField($"Nodes: {_rail.nodes.Count}  Edges: {_rail.edges.Count}");
            if (GUILayout.Button("Frame Rail Network"))
                FrameNetwork(_rail);
        }

        DrawRailBuildTools();
    }

    private void DrawGridTab()
    {
        _grid = (GridNetworkAsset)EditorGUILayout.ObjectField("Grid Network", _grid, typeof(GridNetworkAsset), false);
        if (_grid == null && Selection.activeObject is GridNetworkAsset selectedGrid)
            _grid = selectedGrid;

        if (GUILayout.Button("Create Grid Network Asset"))
            _grid = CreateAsset<GridNetworkAsset>("GridNetwork.asset");

        if (_grid == null)
            return;

        _grid.gridSize = EditorGUILayout.Vector2IntField("Grid Size", _grid.gridSize);
        _grid.cellSize = EditorGUILayout.FloatField("Cell Size", _grid.cellSize);
        _grid.allowDiagonal = EditorGUILayout.Toggle("Allow Diagonal", _grid.allowDiagonal);
        if (GUI.changed)
            EditorUtility.SetDirty(_grid);
    }

    private void DrawEdgeTools(PathGraphBase network)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Edge Authoring", EditorStyles.boldLabel);
        _edgeSplineOverride = (SplineContainer)EditorGUILayout.ObjectField("Edge Spline Override", _edgeSplineOverride, typeof(SplineContainer), true);
        _autoCreateSplineIfMissing = EditorGUILayout.Toggle("Auto Create If Missing", _autoCreateSplineIfMissing);
        _createdSplineLength = Mathf.Max(1f, EditorGUILayout.FloatField("Created Spline Length", _createdSplineLength));
        _selectCreatedSpline = EditorGUILayout.Toggle("Select Created Spline", _selectCreatedSpline);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected Spline As Edge"))
                TryAddSelectedSplineAsEdge(network);

            if (GUILayout.Button("Auto Fill Override"))
                AutoFillSplineOverrideFromSelection();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(network == null))
            {
                if (GUILayout.Button("Create Managed Spline + Edge"))
                    CreateManagedSplineAndAddEdge(network);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Override"))
            {
                _edgeSplineOverride = null;
                SetEdgeStatus("Spline override cleared.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(_edgeSplineOverride == null))
            {
                if (GUILayout.Button("Ping Override"))
                    EditorGUIUtility.PingObject(_edgeSplineOverride);
            }
        }

        EditorGUILayout.HelpBox(_edgeStatus, _edgeStatusType);
        EditorGUILayout.EndVertical();
    }

    private void DrawRoadBuildTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Road Builder", EditorStyles.boldLabel);

        _buildInBackground = EditorGUILayout.Toggle("Build In Background", _buildInBackground);
        _roadSegmentPrefab = (GameObject)EditorGUILayout.ObjectField("Segment Prefab", _roadSegmentPrefab, typeof(GameObject), false);
        _roadSpacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Segment Spacing", _roadSpacing));
        _roadAutoSpacingFromPrefab = EditorGUILayout.Toggle("Auto Spacing From Prefab", _roadAutoSpacingFromPrefab);
        if (_roadAutoSpacingFromPrefab)
            _roadSpacingMultiplier = Mathf.Max(0.01f, EditorGUILayout.FloatField("Spacing Multiplier", _roadSpacingMultiplier));
        _roadAlignToSpline = EditorGUILayout.Toggle("Align To Spline", _roadAlignToSpline);

        _roadConformToTerrain = EditorGUILayout.Toggle("Conform To Terrain", _roadConformToTerrain);
        if (_roadConformToTerrain)
        {
            _roadTerrainMask = DrawLayerMaskField("Terrain Mask", _roadTerrainMask);
            _roadRayStartHeight = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ray Start Height", _roadRayStartHeight));
            _roadRayDistance = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ray Distance", _roadRayDistance));
            _roadTerrainOffset = EditorGUILayout.FloatField("Terrain Offset", _roadTerrainOffset);
            _roadAlignToTerrainNormal = EditorGUILayout.Toggle("Align To Terrain Normal", _roadAlignToTerrainNormal);
        }

        if (GUILayout.Button("Build Road From Network"))
            BuildRoadFromNetwork();

        EditorGUILayout.EndVertical();
    }

    private void DrawRailBuildTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Rail Builder", EditorStyles.boldLabel);

        _buildInBackground = EditorGUILayout.Toggle("Build In Background", _buildInBackground);
        _railTrackPrefab = (GameObject)EditorGUILayout.ObjectField("Track Prefab", _railTrackPrefab, typeof(GameObject), false);
        _railSleeperPrefab = (GameObject)EditorGUILayout.ObjectField("Sleeper Prefab", _railSleeperPrefab, typeof(GameObject), false);
        _railTrackSpacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Track Spacing", _railTrackSpacing));
        _railSleeperSpacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Sleeper Spacing", _railSleeperSpacing));
        _railAutoTrackSpacing = EditorGUILayout.Toggle("Auto Track Spacing", _railAutoTrackSpacing);
        if (_railAutoTrackSpacing)
            _railTrackSpacingMultiplier = Mathf.Max(0.01f, EditorGUILayout.FloatField("Track Spacing Multiplier", _railTrackSpacingMultiplier));
        _railAutoSleeperSpacing = EditorGUILayout.Toggle("Auto Sleeper Spacing", _railAutoSleeperSpacing);
        if (_railAutoSleeperSpacing)
            _railSleeperSpacingMultiplier = Mathf.Max(0.01f, EditorGUILayout.FloatField("Sleeper Spacing Multiplier", _railSleeperSpacingMultiplier));
        _railAlignToSpline = EditorGUILayout.Toggle("Align To Spline", _railAlignToSpline);

        _railConformToTerrain = EditorGUILayout.Toggle("Conform To Terrain", _railConformToTerrain);
        if (_railConformToTerrain)
        {
            _railTerrainMask = DrawLayerMaskField("Terrain Mask", _railTerrainMask);
            _railRayStartHeight = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ray Start Height", _railRayStartHeight));
            _railRayDistance = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ray Distance", _railRayDistance));
            _railTerrainOffset = EditorGUILayout.FloatField("Terrain Offset", _railTerrainOffset);
            _railAlignToTerrainNormal = EditorGUILayout.Toggle("Align To Terrain Normal", _railAlignToTerrainNormal);
        }

        if (GUILayout.Button("Build Rail From Network"))
            BuildRailFromNetwork();

        EditorGUILayout.EndVertical();
    }

    private void TryAddSelectedSplineAsEdge(PathGraphBase network)
    {
        if (network == null)
        {
            SetEdgeStatus("Select or create a network asset first.", MessageType.Warning);
            return;
        }

        if (!TryResolveSplineContainer(out var spline, out string source))
        {
            if (_autoCreateSplineIfMissing && TryCreateManagedSpline(network, out spline, out source))
            {
                // use created spline
            }
            else
            {
                SetEdgeStatus("No SplineContainer found. Select a spline object (or a child/parent in its hierarchy), drag one into Edge Spline Override, or enable auto-create.", MessageType.Warning);
                return;
            }
        }

        if (spline.Spline == null || spline.Spline.Count < 2)
        {
            SetEdgeStatus($"Spline '{spline.name}' must contain at least 2 knots before it can be added as an edge.", MessageType.Warning);
            return;
        }

        Vector3 start = spline.transform.TransformPoint(spline.Spline.EvaluatePosition(0f));
        Vector3 end = spline.transform.TransformPoint(spline.Spline.EvaluatePosition(1f));

        Undo.RecordObject(network, "Add Network Edge");
        int nodeA = EnsureNode(network, start);
        int nodeB = EnsureNode(network, end);
        int edgeId = network.edges.Count == 0 ? 1 : network.edges[^1].id + 1;

        float speedLimit = 15f;
        if (network is RoadNetworkAsset road)
            speedLimit = Mathf.Max(1f, road.defaultSpeedLimit);
        else if (network is RailNetworkAsset rail)
            speedLimit = Mathf.Max(1f, rail.defaultSpeedLimit);

        network.edges.Add(new PathGraphBase.Edge
        {
            id = edgeId,
            from = nodeA,
            to = nodeB,
            spline = spline,
            speedLimit = speedLimit
        });

        EditorUtility.SetDirty(network);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        SetEdgeStatus($"Added edge {edgeId} from {source}: node {nodeA} -> node {nodeB}.", MessageType.Info);
    }

    private void CreateManagedSplineAndAddEdge(PathGraphBase network)
    {
        if (network == null)
        {
            SetEdgeStatus("Select or create a network asset first.", MessageType.Warning);
            return;
        }

        if (!TryCreateManagedSpline(network, out var spline, out string source))
        {
            SetEdgeStatus("Failed to create managed spline.", MessageType.Error);
            return;
        }

        _edgeSplineOverride = spline;
        SetEdgeStatus($"Created {source}.", MessageType.Info);
        TryAddSelectedSplineAsEdge(network);
    }

    private void AutoFillSplineOverrideFromSelection()
    {
        if (!TryResolveSplineFromSelection(out var spline, out string source))
        {
            SetEdgeStatus("Could not find a SplineContainer in current selection, parent chain, or children.", MessageType.Warning);
            return;
        }

        _edgeSplineOverride = spline;
        SetEdgeStatus($"Using spline override from {source}.", MessageType.Info);
    }

    private bool TryResolveSplineContainer(out SplineContainer spline, out string source)
    {
        if (_edgeSplineOverride != null)
        {
            spline = _edgeSplineOverride;
            source = $"override '{_edgeSplineOverride.name}'";
            return true;
        }

        return TryResolveSplineFromSelection(out spline, out source);
    }

    private static bool TryResolveSplineFromSelection(out SplineContainer spline, out string source)
    {
        spline = null;
        source = null;

        var selected = Selection.activeGameObject;
        if (selected == null)
            return false;

        if (selected.TryGetComponent(out spline))
        {
            source = $"selected object '{selected.name}'";
            return true;
        }

        Transform parent = selected.transform.parent;
        while (parent != null)
        {
            if (parent.TryGetComponent(out spline))
            {
                source = $"parent '{parent.name}'";
                return true;
            }

            parent = parent.parent;
        }

        spline = selected.GetComponentInChildren<SplineContainer>(includeInactive: true);
        if (spline != null)
        {
            source = $"child '{spline.name}'";
            return true;
        }

        return false;
    }

    private bool TryCreateManagedSpline(PathGraphBase network, out SplineContainer spline, out string source)
    {
        spline = null;
        source = null;
        if (network == null)
            return false;

        var root = EnsureSplineRoot(network);
        int nextEdgeId = network.edges.Count == 0 ? 1 : network.edges[^1].id + 1;
        string splineName = $"EdgeSpline_{nextEdgeId}";
        var splineObject = EnsureChild(root, splineName).gameObject;

        spline = splineObject.GetComponent<SplineContainer>();
        if (spline == null)
            spline = splineObject.AddComponent<SplineContainer>();

        Vector3 start = GetCreatedSplineStart(network);
        Vector3 direction = GetCreatedSplineForward();
        Vector3 end = start + direction * Mathf.Max(1f, _createdSplineLength);
        ConfigureSplineAsLine(spline, start, end);

        _edgeSplineOverride = spline;
        Undo.RecordObject(network, "Create Managed Spline");
        EditorUtility.SetDirty(spline);
        MarkActiveSceneDirty();
        SceneView.RepaintAll();

        if (_selectCreatedSpline)
            Selection.activeGameObject = splineObject;

        source = $"managed spline '{splineObject.name}'";
        return true;
    }

    private Transform EnsureSplineRoot(PathGraphBase network)
    {
        string type = network is RailNetworkAsset ? "Rail" : "Road";
        var sceneRoot = GameObject.Find("Scene");
        if (sceneRoot == null)
            sceneRoot = new GameObject("Scene");

        var uvsRoot = EnsureChild(sceneRoot.transform, "UVS_Network_Splines");
        var typeRoot = EnsureChild(uvsRoot, type);
        return EnsureChild(typeRoot, string.IsNullOrWhiteSpace(network.name) ? "UnnamedNetwork" : network.name);
    }

    private Vector3 GetCreatedSplineStart(PathGraphBase network)
    {
        if (network != null && network.nodes != null && network.nodes.Count > 0)
            return network.nodes[^1].position;

        if (SceneView.lastActiveSceneView != null)
            return SceneView.lastActiveSceneView.pivot;

        return Vector3.zero;
    }

    private static Vector3 GetCreatedSplineForward()
    {
        if (SceneView.lastActiveSceneView?.camera != null)
        {
            var forward = SceneView.lastActiveSceneView.camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                return forward.normalized;
        }

        return Vector3.forward;
    }

    private static void ConfigureSplineAsLine(SplineContainer container, Vector3 startWorld, Vector3 endWorld)
    {
        var t = container.transform;
        t.position = startWorld;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;

        var spline = container.Spline;
        spline.Clear();
        spline.Add(new BezierKnot(float3.zero));
        spline.Add(new BezierKnot((float3)(endWorld - startWorld)));
        spline.Closed = false;
    }

    private int EnsureNode(PathGraphBase network, Vector3 position)
    {
        const float snap = 0.5f;
        foreach (var n in network.nodes)
        {
            if (Vector3.Distance(n.position, position) <= snap)
                return n.id;
        }

        int newId = network.nodes.Count == 0 ? 1 : network.nodes[^1].id + 1;
        network.nodes.Add(new PathGraphBase.Node { id = newId, position = position });
        return newId;
    }

    private void BuildRoadFromNetwork()
    {
        if (_road == null)
        {
            EditorUtility.DisplayDialog("UVS", "Select a Road Network asset first.", "OK");
            return;
        }
        if (_roadSegmentPrefab == null)
        {
            EditorUtility.DisplayDialog("UVS", "Assign a Road Segment Prefab before building.", "OK");
            return;
        }

        var root = EnsureBuildRoot("Road", _road.name);
        var validEdgeIds = new HashSet<int>(_road.edges.Where(e => e != null).Select(e => e.id));
        CleanupOrphanEdgeObjects(root, validEdgeIds);

        var tasks = new List<UVSNetworkBuildTask>();
        foreach (var edge in _road.edges)
        {
            if (edge == null || edge.spline == null)
                continue;

            var edgeRoot = EnsureChild(root, $"Edge_{edge.id}");
            var builder = edgeRoot.GetComponent<RoadBuilder>();
            if (builder == null)
                builder = edgeRoot.gameObject.AddComponent<RoadBuilder>();

            ConfigureRoadBuilder(builder, edge.spline);
            tasks.Add(new UVSNetworkBuildTask($"Road Edge {edge.id}", builder.Rebuild));
        }

        StartBuild("Build Road Network", tasks);
    }

    private void BuildRailFromNetwork()
    {
        if (_rail == null)
        {
            EditorUtility.DisplayDialog("UVS", "Select a Rail Network asset first.", "OK");
            return;
        }
        if (_railTrackPrefab == null && _railSleeperPrefab == null)
        {
            EditorUtility.DisplayDialog("UVS", "Assign Track and/or Sleeper prefab before building.", "OK");
            return;
        }

        var root = EnsureBuildRoot("Rail", _rail.name);
        var validEdgeIds = new HashSet<int>(_rail.edges.Where(e => e != null).Select(e => e.id));
        CleanupOrphanEdgeObjects(root, validEdgeIds);

        var tasks = new List<UVSNetworkBuildTask>();
        foreach (var edge in _rail.edges)
        {
            if (edge == null || edge.spline == null)
                continue;

            var edgeRoot = EnsureChild(root, $"Edge_{edge.id}");
            var builder = edgeRoot.GetComponent<RailTrackBuilder>();
            if (builder == null)
                builder = edgeRoot.gameObject.AddComponent<RailTrackBuilder>();

            ConfigureRailBuilder(builder, edge.spline);
            tasks.Add(new UVSNetworkBuildTask($"Rail Edge {edge.id}", builder.Rebuild));
        }

        StartBuild("Build Rail Network", tasks);
    }

    private void ConfigureRoadBuilder(RoadBuilder builder, SplineContainer spline)
    {
        builder.spline = spline;
        builder.segmentPrefab = _roadSegmentPrefab;
        builder.spacing = _roadSpacing;
        builder.autoSpacingFromPrefab = _roadAutoSpacingFromPrefab;
        builder.spacingMultiplier = _roadSpacingMultiplier;
        builder.alignToSpline = _roadAlignToSpline;
        builder.conformToTerrain = _roadConformToTerrain;
        builder.terrainMask = _roadTerrainMask;
        builder.rayStartHeight = _roadRayStartHeight;
        builder.rayDistance = _roadRayDistance;
        builder.terrainOffset = _roadTerrainOffset;
        builder.alignToTerrainNormal = _roadAlignToTerrainNormal;
        EditorUtility.SetDirty(builder);
    }

    private void ConfigureRailBuilder(RailTrackBuilder builder, SplineContainer spline)
    {
        builder.spline = spline;
        builder.trackPrefab = _railTrackPrefab;
        builder.sleeperPrefab = _railSleeperPrefab;
        builder.trackSpacing = _railTrackSpacing;
        builder.sleeperSpacing = _railSleeperSpacing;
        builder.autoTrackSpacingFromPrefab = _railAutoTrackSpacing;
        builder.trackSpacingMultiplier = _railTrackSpacingMultiplier;
        builder.autoSleeperSpacingFromPrefab = _railAutoSleeperSpacing;
        builder.sleeperSpacingMultiplier = _railSleeperSpacingMultiplier;
        builder.alignToSpline = _railAlignToSpline;
        builder.conformToTerrain = _railConformToTerrain;
        builder.terrainMask = _railTerrainMask;
        builder.rayStartHeight = _railRayStartHeight;
        builder.rayDistance = _railRayDistance;
        builder.terrainOffset = _railTerrainOffset;
        builder.alignToTerrainNormal = _railAlignToTerrainNormal;
        EditorUtility.SetDirty(builder);
    }

    private void StartBuild(string title, List<UVSNetworkBuildTask> tasks)
    {
        if (tasks == null || tasks.Count == 0)
        {
            Debug.LogWarning($"[{title}] No valid edges with splines were found.");
            return;
        }

        if (_buildInBackground)
        {
            UVSNetworkBuildQueue.Start(title, tasks, success => OnBuildFinished(title, success));
            return;
        }

        foreach (var task in tasks)
            task.Action.Invoke();

        OnBuildFinished(title, true);
    }

    private void OnBuildFinished(string title, bool success)
    {
        AssetDatabase.SaveAssets();
        MarkActiveSceneDirty();
        SceneView.RepaintAll();
        Repaint();

        if (success)
            Debug.Log($"[{title}] Completed.");
        else
            Debug.LogWarning($"[{title}] Cancelled.");
    }

    private static Transform EnsureBuildRoot(string typeName, string networkName)
    {
        var sceneRoot = GameObject.Find("Scene");
        if (sceneRoot == null)
            sceneRoot = new GameObject("Scene");

        var generatedRoot = EnsureChild(sceneRoot.transform, "UVS_Network_Generated");
        var typeRoot = EnsureChild(generatedRoot, typeName);
        return EnsureChild(typeRoot, string.IsNullOrWhiteSpace(networkName) ? "UnnamedNetwork" : networkName);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
            return child;

        var go = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(go, "Create Network Build Root");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void CleanupOrphanEdgeObjects(Transform networkRoot, HashSet<int> validEdgeIds)
    {
        if (networkRoot == null)
            return;

        for (int i = networkRoot.childCount - 1; i >= 0; i--)
        {
            var child = networkRoot.GetChild(i);
            if (!TryParseEdgeId(child.name, out int edgeId))
                continue;

            if (validEdgeIds.Contains(edgeId))
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static bool TryParseEdgeId(string name, out int edgeId)
    {
        edgeId = -1;
        const string prefix = "Edge_";
        if (!name.StartsWith(prefix))
            return false;

        return int.TryParse(name[prefix.Length..], out edgeId);
    }

    private void DrawSceneDebugControls()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Scene Debug", EditorStyles.boldLabel);
        _showSceneDebug = EditorGUILayout.Toggle("Show Network Gizmos", _showSceneDebug);
        _showEdges = EditorGUILayout.Toggle("Show Edges", _showEdges);
        _showNodeIds = EditorGUILayout.Toggle("Show Node IDs", _showNodeIds);
        _editNodes = EditorGUILayout.Toggle("Edit Nodes", _editNodes);
        _nodeSize = EditorGUILayout.Slider("Node Size", _nodeSize, 0.1f, 1.2f);
        _edgeWidth = EditorGUILayout.Slider("Edge Width", _edgeWidth, 1f, 6f);
        _roadNodeColor = EditorGUILayout.ColorField("Road Node Color", _roadNodeColor);
        _railNodeColor = EditorGUILayout.ColorField("Rail Node Color", _railNodeColor);
        _edgeColor = EditorGUILayout.ColorField("Edge Color", _edgeColor);
        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!_showSceneDebug)
            return;

        PathGraphBase active = GetActiveNetwork();
        if (active == null || active.nodes == null)
            return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        if (_showEdges && active.edges != null)
            DrawEdgeGizmos(active);

        bool movedAnyNode = false;
        foreach (var node in active.nodes)
        {
            DrawNodeGizmo(active, node);
            if (!_editNodes)
                continue;

            EditorGUI.BeginChangeCheck();
            Vector3 movedPosition = Handles.PositionHandle(node.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(active, "Move Network Node");
                node.position = movedPosition;
                EditorUtility.SetDirty(active);
                movedAnyNode = true;
            }
        }

        if (movedAnyNode)
            MarkActiveSceneDirty();
    }

    private void DrawEdgeGizmos(PathGraphBase network)
    {
        Handles.color = _edgeColor;
        var points = new List<Vector3>(64);
        foreach (var edge in network.edges)
        {
            if (edge == null)
                continue;

            points.Clear();
            if (TrySampleSpline(edge, points))
            {
                if (points.Count > 1)
                    Handles.DrawAAPolyLine(_edgeWidth, points.ToArray());
                continue;
            }

            if (!TryGetNodePosition(network, edge.from, out var fromPos)) continue;
            if (!TryGetNodePosition(network, edge.to, out var toPos)) continue;
            Handles.DrawAAPolyLine(_edgeWidth, fromPos, toPos);
        }
    }

    private void DrawNodeGizmo(PathGraphBase network, PathGraphBase.Node node)
    {
        bool isRail = network is RailNetworkAsset;
        Handles.color = isRail ? _railNodeColor : _roadNodeColor;

        if (isRail)
            Handles.CubeHandleCap(0, node.position, Quaternion.identity, _nodeSize, EventType.Repaint);
        else
            Handles.SphereHandleCap(0, node.position, Quaternion.identity, _nodeSize, EventType.Repaint);

        if (_showNodeIds)
            Handles.Label(node.position + Vector3.up * (_nodeSize + 0.15f), $"N{node.id}");
    }

    private static bool TrySampleSpline(PathGraphBase.Edge edge, List<Vector3> points)
    {
        if (edge?.spline == null || edge.spline.Spline == null || points == null)
            return false;

        float length = edge.spline.Spline.GetLength();
        int steps = Mathf.Clamp(Mathf.CeilToInt(length / 2f), 12, 256);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 local = edge.spline.Spline.EvaluatePosition(t);
            points.Add(edge.spline.transform.TransformPoint(local));
        }

        return points.Count > 1;
    }

    private PathGraphBase GetActiveNetwork()
    {
        if (_tab == 0 && _road != null) return _road;
        if (_tab == 1 && _rail != null) return _rail;

        if (Selection.activeObject is PathGraphBase selectedNetwork)
            return selectedNetwork;

        if (_road != null) return _road;
        if (_rail != null) return _rail;
        return null;
    }

    private bool TryGetNodePosition(PathGraphBase network, int id, out Vector3 position)
    {
        position = Vector3.zero;
        if (network == null || network.nodes == null)
            return false;

        foreach (var node in network.nodes)
        {
            if (node.id != id) continue;
            position = node.position;
            return true;
        }

        return false;
    }

    private void FrameNetwork(PathGraphBase network)
    {
        if (network == null || network.nodes == null || network.nodes.Count == 0)
            return;

        var bounds = new Bounds(network.nodes[0].position, Vector3.one * 0.01f);
        foreach (var node in network.nodes)
            bounds.Encapsulate(node.position);

        SceneView.lastActiveSceneView?.Frame(bounds, false);
        SceneView.RepaintAll();
    }

    private T CreateAsset<T>(string defaultName) where T : ScriptableObject
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Network Asset", defaultName, "asset", "Choose asset location");
        if (string.IsNullOrEmpty(path))
            return null;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        return asset;
    }

    private static LayerMask DrawLayerMaskField(string label, LayerMask selected)
    {
        var layers = InternalEditorUtility.layers;
        int maskWithoutEmpty = 0;

        for (int i = 0; i < layers.Length; i++)
        {
            int layerIndex = LayerMask.NameToLayer(layers[i]);
            if (layerIndex >= 0 && ((selected.value & (1 << layerIndex)) != 0))
                maskWithoutEmpty |= 1 << i;
        }

        int newMaskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);
        int newMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if ((newMaskWithoutEmpty & (1 << i)) == 0)
                continue;

            int layerIndex = LayerMask.NameToLayer(layers[i]);
            if (layerIndex >= 0)
                newMask |= 1 << layerIndex;
        }

        selected.value = newMask;
        return selected;
    }

    private void SetEdgeStatus(string message, MessageType type)
    {
        _edgeStatus = message;
        _edgeStatusType = type;
        Repaint();
    }

    private static void MarkActiveSceneDirty()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
