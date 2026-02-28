using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public abstract class PathGraphBase : ScriptableObject
{
    [Serializable]
    public class Node
    {
        public int id;
        public Vector3 position;
    }

    [Serializable]
    public class Edge
    {
        public int id;
        public int from;
        public int to;
        public bool bidirectional = true;
        public SplineContainer spline;
        public float speedLimit = 15f;
        public List<string> tags = new();
    }

    [Serializable]
    public class Route
    {
        public List<int> edgeIds = new();
        public List<Vector3> sampledPoints = new();
    }

    public List<Node> nodes = new();
    public List<Edge> edges = new();
    public float sampleStep = 2f;

    public bool TryFindRoute(int fromNodeId, int toNodeId, out Route route)
    {
        route = new Route();
        if (fromNodeId == toNodeId) return true;

        var nodeMap = nodes.ToDictionary(n => n.id, n => n);
        if (!nodeMap.ContainsKey(fromNodeId) || !nodeMap.ContainsKey(toNodeId))
            return false;

        var open = new List<int> { fromNodeId };
        var cameFrom = new Dictionary<int, int>();
        var cameEdge = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float> { [fromNodeId] = 0f };

        while (open.Count > 0)
        {
            int current = open.OrderBy(n => gScore.GetValueOrDefault(n, float.MaxValue) + Heuristic(nodeMap[n], nodeMap[toNodeId])).First();
            if (current == toNodeId)
                break;

            open.Remove(current);
            foreach (var edge in GetOutgoingEdges(current))
            {
                int next = edge.to;
                float tentative = gScore.GetValueOrDefault(current, float.MaxValue) + EdgeCost(nodeMap[current], nodeMap[next], edge);
                if (tentative < gScore.GetValueOrDefault(next, float.MaxValue))
                {
                    cameFrom[next] = current;
                    cameEdge[next] = edge.id;
                    gScore[next] = tentative;
                    if (!open.Contains(next))
                        open.Add(next);
                }
            }
        }

        if (!cameFrom.ContainsKey(toNodeId))
            return false;

        var edgePath = new List<int>();
        int cur = toNodeId;
        while (cameFrom.ContainsKey(cur))
        {
            edgePath.Add(cameEdge[cur]);
            cur = cameFrom[cur];
        }
        edgePath.Reverse();
        route.edgeIds = edgePath;
        BuildRouteSamples(route);
        return true;
    }

    public void BuildRouteSamples(Route route)
    {
        route.sampledPoints.Clear();
        foreach (var edgeId in route.edgeIds)
        {
            var edge = edges.FirstOrDefault(e => e.id == edgeId);
            if (edge == null || edge.spline == null) continue;

            float length = edge.spline.Spline.GetLength();
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / Mathf.Max(0.1f, sampleStep)));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pos = edge.spline.Spline.EvaluatePosition(t);
                route.sampledPoints.Add(edge.spline.transform.TransformPoint(pos));
            }
        }
    }

    private IEnumerable<Edge> GetOutgoingEdges(int nodeId)
    {
        foreach (var edge in edges)
        {
            if (edge.from == nodeId) yield return edge;
            if (edge.bidirectional && edge.to == nodeId)
            {
                yield return new Edge
                {
                    id = edge.id,
                    from = edge.to,
                    to = edge.from,
                    bidirectional = edge.bidirectional,
                    spline = edge.spline,
                    speedLimit = edge.speedLimit,
                    tags = edge.tags
                };
            }
        }
    }

    private static float Heuristic(Node a, Node b)
    {
        return Vector3.Distance(a.position, b.position);
    }

    private static float EdgeCost(Node a, Node b, Edge edge)
    {
        if (edge.spline == null)
            return Vector3.Distance(a.position, b.position);

        return Mathf.Max(1f, edge.spline.Spline.GetLength());
    }
}
