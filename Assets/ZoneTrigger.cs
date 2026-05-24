using UnityEngine;

[ExecuteInEditMode]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] public string zoneName = "Zone1";

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private void Start()
    {
        // OnTriggerStay never fires between two static colliders at scene load,
        // so do the bounds check immediately on Start instead.
        if (Application.isPlaying) AssignZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        MapNode node = other.GetComponent<MapNode>();
        if (node != null) node.zone = zoneName;
    }

    private void OnTriggerStay(Collider other)
    {
        MapNode node = other.GetComponent<MapNode>();
        if (node != null && node.zone != zoneName) node.zone = zoneName;
    }

    // ─── Editor: click this in the Inspector context menu to batch-assign ────

    [ContextMenu("Assign Zone to Overlapping Nodes")]
    public void AssignZone()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[ZoneTrigger] No Collider found on this GameObject.");
            return;
        }

        int count = 0;
        foreach (MapNode node in FindObjectsOfType<MapNode>())
        {
            if (col.bounds.Contains(node.transform.position))
            {
                node.zone = zoneName;
                count++;
            }
        }
        Debug.Log($"[ZoneTrigger] Zone '{zoneName}' assigned to {count} node(s).");
    }

    // ─── Gizmos: shows zone area and label in Scene view ─────────────────────

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.15f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);

        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(col.bounds.center, zoneName);
#endif
    }
}
