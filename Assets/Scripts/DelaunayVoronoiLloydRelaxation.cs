using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using UnityEngine;

public class DelaunayVoronoiLloydRelaxation : MonoBehaviour
{
    [System.Serializable]
    public struct BoundingBox
    {
        public Vector2 center;
        public Vector2 size;
    }
    [Header("Delaunay, Voronoi, Dijkstra Algorithm, Lloyd Relaxation")]
    [Header("Settings")]
    [SerializeField] private BoundingBox bounds = new BoundingBox 
    { 
        center = Vector2.zero,
        size = new Vector2(64, 64)
    };
    [SerializeField] private int pointsCount = 100;
    [SerializeField] private float pointSize = 0.1f;

    [Header("Visualization")]
    [SerializeField] private bool showPoints = true;
    [SerializeField] private Color PointColor = Color.black;
    [SerializeField] private bool showVoronoi = true;
    [SerializeField] private bool showDelaunay;
    [SerializeField] private bool showSpanningTree;
    [SerializeField] private bool enableLloydRelaxation;
    [SerializeField] private bool pauseRelaxation;

    [Header("Pathfinding")]
    [SerializeField] private bool enableDijkstra;
    [SerializeField] private bool showPath;
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private float pathWidth = 3f;
    [SerializeField] private Color startPointColor = Color.yellow;
    [SerializeField] private Color endPointColor = Color.cyan;

    private Camera mainCamera;
    
    // Geometric data
    private List<Vector2> points;
    private List<Vector2> relaxedPoints;
    private List<LineSegment> voronoiEdges;
    private List<LineSegment> delaunayEdges;
    private List<LineSegment> spanningTreeEdges;
    
    // Pathfinding data
    private int startPointIndex = -1;
    private int endPointIndex = -1;
    private List<Vector2> shortestPath;
    private Dictionary<int, Dictionary<int, float>> vertexPoints;
    
    private Rect voronoiRect;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        InitializeGeometry();
    }

    private void InitializeGeometry()
    {
        points = new List<Vector2>(pointsCount);
        voronoiRect = new Rect(
            bounds.center.x - bounds.size.x / 2f,
            bounds.center.y - bounds.size.y / 2f,
            bounds.size.x,
            bounds.size.y
        );

        GenerateRandomPoints();
        UpdateGeometry();
        ResetPathfinding();
    }

    private void GenerateRandomPoints()
    {
        points.Clear();
        for (int i = 0; i < pointsCount; i++)
        {
            points.Add(new Vector2(
                Random.Range(voronoiRect.xMin, voronoiRect.xMax),
                Random.Range(voronoiRect.yMin, voronoiRect.yMax)
            ));
        }
    }

    private void Update()
    {
        HandleInput();
        
        if (enableLloydRelaxation && !pauseRelaxation)
        {
            PerformLloydRelaxation();
            UpdateGeometry();
            if (enableDijkstra && showPath)
            {
                RecalculatePath();
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitializeGeometry();
        }

        if (enableDijkstra)
        {
            if (Input.GetMouseButtonDown(0))
            {
                startPointIndex = FindNearestPointIndex(mainCamera.ScreenToWorldPoint(Input.mousePosition));
                if (startPointIndex >= 0 && endPointIndex >= 0)
                {
                    CalculateShortestPath();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                endPointIndex = FindNearestPointIndex(mainCamera.ScreenToWorldPoint(Input.mousePosition));
                if (startPointIndex >= 0 && endPointIndex >= 0)
                {
                    CalculateShortestPath();
                }
            }
        }
    }

    private int FindNearestPointIndex(Vector2 position)
    {
        int nearestIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector2.SqrMagnitude(points[i] - position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void ResetPathfinding()
    {
        startPointIndex = -1;
        endPointIndex = -1;
        shortestPath = null;
        vertexPoints = null;
    }

    private void BuildAllPoints()
    {
        vertexPoints = new Dictionary<int, Dictionary<int, float>>();

        for (int i = 0; i < points.Count; i++)
        {
            vertexPoints[i] = new Dictionary<int, float>();
        }

        foreach (var edge in delaunayEdges)
        {
            Vector2 p0 = new Vector2(edge.p0.Value.x, edge.p0.Value.y);
            Vector2 p1 = new Vector2(edge.p1.Value.x, edge.p1.Value.y);
            
            int index0 = points.FindIndex(p => Vector2.SqrMagnitude(p - p0) < 0.0001f);
            int index1 = points.FindIndex(p => Vector2.SqrMagnitude(p - p1) < 0.0001f);

            if (index0 >= 0 && index1 >= 0)
            {
                float distance = Vector2.Distance(p0, p1);
                vertexPoints[index0][index1] = distance;
                vertexPoints[index1][index0] = distance;
            }
        }
    }

    // Dijkstra's algorithm
    private void CalculateShortestPath()
    {
        if (startPointIndex < 0 || endPointIndex < 0 || startPointIndex == endPointIndex)
        {
            shortestPath = null;
            return;
        }

        if (vertexPoints == null)
        {
            BuildAllPoints();
        }

        Dictionary<int, float> distances = new Dictionary<int, float>();
        Dictionary<int, int> previous = new Dictionary<int, int>();
        HashSet<int> unvisited = new HashSet<int>(); 

        foreach (int vertex in vertexPoints.Keys)
        {
            distances[vertex] = float.MaxValue;
            previous[vertex] = -1;
            unvisited.Add(vertex);
        }
        distances[startPointIndex] = 0;

        while (unvisited.Count > 0)
        {
            int current = -1;
            float minDistance = float.MaxValue;
            foreach (int vertex in unvisited)
            {
                if (distances[vertex] < minDistance)
                {
                    minDistance = distances[vertex];
                    current = vertex;
                }
            }

            if (current == -1 || current == endPointIndex)
                break;

            unvisited.Remove(current);

            foreach (var neighbor in vertexPoints[current])
            {
                if (!unvisited.Contains(neighbor.Key))
                    continue;

                float newDistance = distances[current] + neighbor.Value;
                if (newDistance < distances[neighbor.Key])
                {
                    distances[neighbor.Key] = newDistance;
                    previous[neighbor.Key] = current;
                }
            }
        }

        shortestPath = new List<Vector2>();
        int currentNode = endPointIndex;
        while (currentNode != -1)
        {
            shortestPath.Add(points[currentNode]);
            currentNode = previous[currentNode];
        }
        shortestPath.Reverse();
    }

    private void RecalculatePath()
    {
        if (startPointIndex >= 0 && endPointIndex >= 0)
        {
            BuildAllPoints();
            CalculateShortestPath();
        }
    }

    private void PerformLloydRelaxation()
    {
        var voronoi = new Voronoi(points, voronoiRect);
        relaxedPoints = voronoi.LloydRelaxation();
        points = relaxedPoints;
    }

    private void UpdateGeometry()
    {
        var voronoi = new Voronoi(points, voronoiRect);
        
        if (showVoronoi)
        {
            voronoiEdges = voronoi.VoronoiDiagram();
        }
        
        if (showDelaunay || showSpanningTree || enableDijkstra)
        {
            delaunayEdges = voronoi.DelaunayTriangulation();
            if (showSpanningTree)
            {
                spanningTreeEdges = DelaunayHelpers.Kruskal(delaunayEdges, KruskalType.MAXIMUM);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        DrawBoundingBox();
        
        if (showPoints)
        {
            DrawPoints();
        }
        
        if (showVoronoi)
        {
            DrawVoronoiEdges();
        }
        
        if (showDelaunay)
        {
            DrawDelaunayEdges();
        }
        
        if (showSpanningTree)
        {
            DrawSpanningTree();
        }

        if (enableDijkstra)
        {
            DrawPathfindingPoints();
            if (showPath && shortestPath != null && shortestPath.Count > 1)
            {
                DrawPath();
            }
        }
    }

    private void DrawPathfindingPoints()
    {
        if (startPointIndex >= 0)
        {
            Gizmos.color = startPointColor;
            Gizmos.DrawWireSphere(points[startPointIndex], pointSize * 2);
        }
        if (endPointIndex >= 0)
        {
            Gizmos.color = endPointColor;
            Gizmos.DrawWireSphere(points[endPointIndex], pointSize * 2);
        }
    }

    private void DrawPath()
    {
        Gizmos.color = pathColor;
        for (int i = 0; i < shortestPath.Count - 1; i++)
        {
            Vector3 start = shortestPath[i];
            Vector3 end = shortestPath[i + 1];
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * pathWidth * 0.1f;
            
            for (int j = -1; j <= 1; j++)
            {
                Vector3 offset = perpendicular * j * 0.5f;
                Gizmos.DrawLine(start + offset, end + offset);
            }
        }
    }

    private void DrawBoundingBox()
    {
        Gizmos.color = Color.black;
        Vector2 min = new Vector2(voronoiRect.xMin, voronoiRect.yMin);
        Vector2 max = new Vector2(voronoiRect.xMax, voronoiRect.yMax);
        
        Gizmos.DrawLine(new Vector2(min.x, min.y), new Vector2(min.x, max.y));
        Gizmos.DrawLine(new Vector2(min.x, min.y), new Vector2(max.x, min.y));
        Gizmos.DrawLine(new Vector2(max.x, min.y), new Vector2(max.x, max.y));
        Gizmos.DrawLine(new Vector2(min.x, max.y), new Vector2(max.x, max.y));
    }

    private void DrawPoints()
    {
        Gizmos.color = PointColor;
        foreach (var point in points)
        {
            Gizmos.DrawSphere(point, pointSize);
        }
    }

    private void DrawVoronoiEdges()
    {
        if (voronoiEdges == null) return;
        
        Gizmos.color = Color.black;
        foreach (var edge in voronoiEdges)
        {
            Gizmos.DrawLine(
                new Vector2(edge.p0.Value.x, edge.p0.Value.y),
                new Vector2(edge.p1.Value.x, edge.p1.Value.y)
            );
        }
    }

    private void DrawDelaunayEdges()
    {
        if (delaunayEdges == null) return;
        
        Gizmos.color = Color.green;
        foreach (var edge in delaunayEdges)
        {
            Gizmos.DrawLine(
                new Vector2(edge.p0.Value.x, edge.p0.Value.y),
                new Vector2(edge.p1.Value.x, edge.p1.Value.y)
            );
        }
    }

    private void DrawSpanningTree()
    {
        if (spanningTreeEdges == null) return;
        
        Gizmos.color = Color.red;
        foreach (var edge in spanningTreeEdges)
        {
            Gizmos.DrawLine(
                new Vector2(edge.p0.Value.x, edge.p0.Value.y),
                new Vector2(edge.p1.Value.x, edge.p1.Value.y)
            );
        }
    }
}