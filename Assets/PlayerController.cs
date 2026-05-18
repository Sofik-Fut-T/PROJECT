using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public MapNode currentNode; // Та плитка, на якій кулька ЗАРАЗ
    private bool isMoving = false;

    void Start()
    {
        if (currentNode != null)
        {
            transform.position = currentNode.transform.position + Vector3.up * 0.05f;
        }
    }

    void Update()
    {
        if (isMoving) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Беремо шар "Nodes", щоб ігнорувати острів Mesh_0
            int nodesLayer = LayerMask.GetMask("Nodes");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, nodesLayer))
            {
                MapNode clickedNode = hit.collider.GetComponent<MapNode>();

                if (clickedNode != null)
                {
                    // ЦЕЙ РЯДОК ЗАБОРОНЯЄ ЛІТАТИ ЧЕРЕЗ ПІВ КАРТИ:
                    if (currentNode.neighbors.Contains(clickedNode))
                    {
                        StartCoroutine(MoveToNode(clickedNode));
                    }
                    else
                    {
                        Debug.LogWarning("Занадто далеко! Це не сусідній вузол.");
                    }
                }
            }
        }
    }

    IEnumerator MoveToNode(MapNode targetNode)
    {
        isMoving = true;
        Vector3 targetPos = targetNode.transform.position + Vector3.up * 0.05f;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        currentNode = targetNode; // Тепер ця плитка стає нашою поточною
        isMoving = false;
    }
}