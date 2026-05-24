using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MapNode : MonoBehaviour
{
    [Header("Node Settings")]
    public float checkRadius = 5f;
    public List<MapNode> neighbors = new List<MapNode>();

    [Header("Zone")]
    [SerializeField] public string zone;

    // ������ ��� ��������� (��� �� ���� ������� -> Update Neighbors)
    [ContextMenu("Update Neighbors")]
    public void UpdateNeighbors()
    {
        neighbors.Clear();
        MapNode[] allNodes = Object.FindObjectsOfType<MapNode>();

        foreach (MapNode node in allNodes)
        {
            // �������� ���� �� ��'����, ���� ���� �� �����
            if (node == this || node.gameObject.scene.name == null) continue;

            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance <= checkRadius)
            {
                neighbors.Add(node);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(zone))
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, zone);
#endif
    }
}