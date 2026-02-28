using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class JunctionBuilder : MonoBehaviour
{
    public PathGraphBase graph;
    public GameObject junctionPrefab;

    [ContextMenu("Rebuild Junctions")]
    public void Rebuild()
    {
        if (graph == null || junctionPrefab == null) return;

        ClearChildren();

        foreach (var node in graph.nodes)
        {
            int connections = graph.edges.Count(e => e.from == node.id || e.to == node.id);
            if (connections < 3) continue;

            var go = Instantiate(junctionPrefab, node.position, Quaternion.identity, transform);
            go.name = $"{junctionPrefab.name}_{node.id}";
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }
}
