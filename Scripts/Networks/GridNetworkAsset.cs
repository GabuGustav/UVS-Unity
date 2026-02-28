using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UVS/Networks/Grid Network", fileName = "GridNetwork")]
public class GridNetworkAsset : ScriptableObject
{
    public Vector2Int gridSize = new Vector2Int(32, 32);
    public float cellSize = 2f;
    public bool allowDiagonal = true;

    public bool TryFindPath(Vector2Int start, Vector2Int goal, out List<Vector3> worldPoints)
    {
        worldPoints = new List<Vector3>();
        if (!InBounds(start) || !InBounds(goal)) return false;

        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };

        while (open.Count > 0)
        {
            Vector2Int current = open[0];
            int best = gScore[current] + Heuristic(current, goal);
            for (int i = 1; i < open.Count; i++)
            {
                var n = open[i];
                int score = gScore[n] + Heuristic(n, goal);
                if (score < best)
                {
                    best = score;
                    current = n;
                }
            }

            if (current == goal) break;
            open.Remove(current);

            foreach (var next in GetNeighbors(current))
            {
                int tentative = gScore[current] + 1;
                if (!gScore.ContainsKey(next) || tentative < gScore[next])
                {
                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    if (!open.Contains(next))
                        open.Add(next);
                }
            }
        }

        if (!cameFrom.ContainsKey(goal) && goal != start)
            return false;

        var path = new List<Vector2Int>();
        var cur = goal;
        path.Add(cur);
        while (cameFrom.ContainsKey(cur))
        {
            cur = cameFrom[cur];
            path.Add(cur);
        }
        path.Reverse();

        foreach (var p in path)
            worldPoints.Add(GridToWorld(p));

        return true;
    }

    private bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.y >= 0 && p.x < gridSize.x && p.y < gridSize.y;
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
    {
        yield return new Vector2Int(p.x + 1, p.y);
        yield return new Vector2Int(p.x - 1, p.y);
        yield return new Vector2Int(p.x, p.y + 1);
        yield return new Vector2Int(p.x, p.y - 1);

        if (!allowDiagonal) yield break;
        yield return new Vector2Int(p.x + 1, p.y + 1);
        yield return new Vector2Int(p.x - 1, p.y + 1);
        yield return new Vector2Int(p.x + 1, p.y - 1);
        yield return new Vector2Int(p.x - 1, p.y - 1);
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }
}
