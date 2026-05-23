using System.Collections;
using Mirror;
using UnityEngine;

public abstract class CharacterBase : NetworkBehaviour
{
    protected bool IsNeighbor(int from, int to) =>
        GridManager.Instance.AreNeighbors(from, to);

    [ClientRpc]
    protected void RpcSmoothMoveTo(Vector3 worldPos)
    {
        StartCoroutine(SmoothMove(worldPos));
    }

    private IEnumerator SmoothMove(Vector3 target)
    {
        const float speed = 6f;
        while (Vector3.Distance(transform.position, target) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}
