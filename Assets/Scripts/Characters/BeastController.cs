using Mirror;
using UnityEngine;

public class BeastController : CharacterBase
{
    // Server-only — never sent to hunter clients
    private int _serverNodeId = -1;
    public  int ServerNodeId => _serverNodeId;

    [SerializeField] private Transform  currentNodeTransform;
    [SerializeField] private GameObject localPlayerIndicatorPrefab;

    private int  _dashCD, _fakeCD, _eraseCD;
    private bool _movedThisTurn;
    [SyncVar] public int dashCooldown;
    [SyncVar] public int fakeCooldown;
    [SyncVar] public int eraseCooldown;

    public override void OnStartClient()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = isOwned;
        }
    }

    // Called when this client gains authority over this object (i.e. this is the beast player)
    public override void OnStartAuthority()
    {
        NodeInput nodeInput = FindFirstObjectByType<NodeInput>();
        if (nodeInput != null) nodeInput.Init(this, null);

        if (localPlayerIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(localPlayerIndicatorPrefab, transform);
            indicator.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.1f, 0f), Quaternion.identity);
        }
    }

    // ─── Commands ────────────────────────────────────────────────────────────

    [Command]
    public void CmdMove(int targetNodeId)
    {
        Debug.Log($"[Beast.CmdMove] target={targetNodeId} currentNode={_serverNodeId} " +
                  $"IsBeastTurn={TurnManager.Instance != null && TurnManager.Instance.IsBeastTurn}");

        if (!TurnManager.Instance.IsBeastTurn)
        {
            Debug.Log("[Beast.CmdMove] Відхилено — не хід Звіра");
            return;
        }

        if (_movedThisTurn) return;

        if (_serverNodeId == -1)
        {
            // First click: beast chooses starting position, any valid node accepted
            if (!GridManager.Instance.IsValid(targetNodeId)) return;
            _movedThisTurn = true;
            SetStartNode(targetNodeId);
            TurnManager.Instance.OnBeastMoved(this);
            return;
        }

        bool neighbor = IsNeighbor(_serverNodeId, targetNodeId);
        Debug.Log($"[Beast.CmdMove] IsNeighbor({_serverNodeId},{targetNodeId})={neighbor}");

        if (!neighbor) return;

        _movedThisTurn = true;
        SetNodeId(targetNodeId);
        RpcSetPosition(GridManager.Instance.NodeToWorld(targetNodeId));
        TurnManager.Instance.OnBeastMoved(this);
    }

    [Command]
    public void CmdDash(int targetNodeId)
    {
        if (_dashCD > 0 || !TurnManager.Instance.IsBeastTurn) return;
        // Dash: 2 hops away — check that target is a neighbor of a neighbor
        if (!IsTwoHopsAway(_serverNodeId, targetNodeId)) return;

        _movedThisTurn = true;
        _dashCD = 3; dashCooldown = 3;
        SetNodeId(targetNodeId);
        RpcSetPosition(GridManager.Instance.NodeToWorld(targetNodeId));
        TurnManager.Instance.OnBeastMoved(this);   // dash counts as the beast's move
    }

    [Command]
    public void CmdFakeTrail()
    {
        if (_fakeCD > 0 || !TurnManager.Instance.IsBeastTurn) return;
        _fakeCD = 3; fakeCooldown = 3;
        ClueManager.Instance.SpawnFakeClue();
        TurnManager.Instance.OnBeastUsedAbility();
    }

    [Command]
    public void CmdErase()
    {
        if (_eraseCD > 0 || !TurnManager.Instance.IsBeastTurn) return;
        _eraseCD = 3; eraseCooldown = 3;
        ClueManager.Instance.EraseLastClue();
        TurnManager.Instance.OnBeastUsedAbility();
    }

    [Command]
    public void CmdSkipAbility() => TurnManager.Instance.OnBeastUsedAbility();

    // ─── Server helpers ───────────────────────────────────────────────────────

    [Server]
    public void BeginTurn() => _movedThisTurn = false;

    [Server]
    public void SetStartNode(int nodeId)
    {
        SetNodeId(nodeId);
        RpcSetPosition(GridManager.Instance.NodeToWorld(nodeId));
    }

    // Moves beast on all clients — hunters won't see it because Renderer is disabled
    [ClientRpc]
    private void RpcSetPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        // Show only to the owner (beast player)
        if (isOwned)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = true;
            }
        }
    }

    [TargetRpc]
    public void TargetNotifyTurn(NetworkConnectionToClient target, bool isFirstPlacement)
    {
        GameHUD.Instance?.SetPhaseText(isFirstPlacement ? "Оберіть початкову позицію!" : "Твій хід, Звіре!");
        AbilityPanel.Instance?.RefreshBeast(this);
    }

    [Server]
    public void TickCooldowns()
    {
        if (_dashCD > 0) { _dashCD--;  dashCooldown  = _dashCD;  }
        if (_fakeCD > 0) { _fakeCD--;  fakeCooldown  = _fakeCD;  }
        if (_eraseCD > 0){ _eraseCD--; eraseCooldown = _eraseCD; }
    }

    private void SetNodeId(int nodeId)
    {
        _serverNodeId = nodeId;
        MapNode node  = GridManager.Instance.GetNode(nodeId);
        currentNodeTransform = node != null ? node.transform : null;
        if (node != null) { transform.position = node.transform.position; }
    }

    private bool IsTwoHopsAway(int from, int to)
    {
        var neighbors = GridManager.Instance.GetNeighborIds(from);
        foreach (int mid in neighbors)
            if (GridManager.Instance.AreNeighbors(mid, to) && mid != to)
                return true;
        return false;
    }

    [ContextMenu("Force Change Position")]
    public void ForceChangePos()
    {
        transform.position = currentNodeTransform.position;
    }
}
