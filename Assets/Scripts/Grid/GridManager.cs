using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Collects all existing MapNode/Plate objects from the scene.
// Does NOT generate anything — the map is already built in the editor.
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    private MapNode[] nodes;
    private readonly Dictionary<MapNode, int> nodeToId = new();

    private void Awake()
    {
        Instance = this;
        RegisterNodes();
    }

    private void RegisterNodes()
    {
        MapNode[] found = FindObjectsByType<MapNode>(FindObjectsSortMode.None);
        // Sort by world position so server and client always produce identical node order.
        // InstanceID is runtime-only and differs between processes — never use it for networking.
        System.Array.Sort(found, (a, b) =>
        {
            Vector3 pa = a.transform.position, pb = b.transform.position;
            int xCmp = pa.x.CompareTo(pb.x);
            return xCmp != 0 ? xCmp : pa.z.CompareTo(pb.z);
        });
        nodes = found;
        nodeToId.Clear();
        for (int i = 0; i < nodes.Length; i++)
            nodeToId[nodes[i]] = i;

        Debug.Log($"[GridManager] Registered {nodes.Length} MapNodes (sorted by position).");
    }

    // ─── Lookups ──────────────────────────────────────────────────────────────

    public int     NodeCount       => nodes?.Length ?? 0;
    public bool    IsValid(int id) => id >= 0 && id < NodeCount;
    public MapNode GetNode(int id) => IsValid(id) ? nodes[id] : null;

    public int GetId(MapNode node) =>
        nodeToId.TryGetValue(node, out int id) ? id : -1;

    public bool AreNeighbors(int from, int to)
    {
        MapNode nodeA = GetNode(from);
        MapNode nodeB = GetNode(to);
        return nodeA != null && nodeB != null && nodeA.neighbors.Contains(nodeB);
    }

    public List<int> GetNeighborIds(int nodeId)
    {
        MapNode node = GetNode(nodeId);
        return node == null
            ? new()
            : node.neighbors.Select(n => GetId(n)).Where(id => id >= 0).ToList();
    }

    public Vector3 NodeToWorld(int nodeId)
    {
        MapNode node = GetNode(nodeId);
        return node != null ? node.transform.position : Vector3.zero;
    }

    public int GetRandomNodeId() =>
        nodes != null && nodes.Length > 0 ? Random.Range(0, nodes.Length) : 0;

    // ─── Highlighting (requires GridCell component on Plate prefab) ───────────

    public void HighlightNode(int nodeId, CellHighlight h)
    {
        MapNode node = GetNode(nodeId);
        if (node == null) return;
        if (node.TryGetComponent(out GridCell cell)) cell.SetHighlight(h);
    }

    public void HighlightNodes(IEnumerable<int> ids, CellHighlight h)
    {
        foreach (int id in ids) HighlightNode(id, h);
    }

    public void ClearHighlights()
    {
        if (nodes == null) return;
        foreach (MapNode node in nodes)
        {
            if (node.TryGetComponent(out GridCell cell))
            {
                cell.SetHighlight(CellHighlight.None);
            }
        }
    }

    // ─── Clue helpers (based on world position of nodes) ─────────────────────

    public List<int> GetNodesInRow(float worldZ, float tolerance = 0.8f)
    {
        List<int> result = new();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (Mathf.Abs(nodes[i].transform.position.z - worldZ) <= tolerance)
                result.Add(i);
        }
        return result;
    }

    public List<int> GetNodesInColumn(float worldX, float tolerance = 0.8f)
    {
        List<int> result = new();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (Mathf.Abs(nodes[i].transform.position.x - worldX) <= tolerance)
                result.Add(i);
        }
        return result;
    }

    public List<int> GetNodesInZone(int centerNodeId, float radius = 2.5f)
    {
        Vector3 center = NodeToWorld(centerNodeId);
        List<int> result = new();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (Vector3.Distance(nodes[i].transform.position, center) <= radius)
                result.Add(i);
        }
        return result;
    }
}
