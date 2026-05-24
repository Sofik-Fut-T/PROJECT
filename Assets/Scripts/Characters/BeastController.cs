using Mirror;
using UnityEngine;

public class BeastController : CharacterBase
{
    // Server-only — never sent to hunter clients
    private int _serverNodeId = -1;
    public  int ServerNodeId => _serverNodeId;

    [SerializeField] private Transform  currentNodeTransform;
    [SerializeField] private GameObject localPlayerIndicatorPrefab;

    private bool _movedThisTurn;
    [SyncVar(hook = nameof(OnDashCDSync))]  public int dashCooldown;
    [SyncVar(hook = nameof(OnFakeCDSync))]  public int fakeCooldown;
    [SyncVar(hook = nameof(OnEraseCDSync))] public int eraseCooldown;

    private void OnDashCDSync(int _, int val)
    {
        if (isOwned && AbilityPanel.Instance != null) { AbilityPanel.Instance.RefreshBeastCooldown(val, 0); }
    }

    private void OnFakeCDSync(int _, int val)
    {
        if (isOwned && AbilityPanel.Instance != null) { AbilityPanel.Instance.RefreshBeastCooldown(val, 1); }
    }

    private void OnEraseCDSync(int _, int val)
    {
        if (isOwned && AbilityPanel.Instance != null) { AbilityPanel.Instance.RefreshBeastCooldown(val, 2); }
    }

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
        if (dashCooldown > 0 || !TurnManager.Instance.IsBeastTurn) return;
        if (_movedThisTurn) return;
        if (!IsTwoHopsAway(_serverNodeId, targetNodeId)) return;

        _movedThisTurn = true;
        dashCooldown = 3;
        SetNodeId(targetNodeId);
        RpcSetPosition(GridManager.Instance.NodeToWorld(targetNodeId));
        TurnManager.Instance.OnBeastMoved(this);
    }

    [Command]
    public void CmdFakeTrail()
    {
        if (fakeCooldown > 0 || !TurnManager.Instance.IsBeastTurn) return;
        fakeCooldown = 3;
        ClueManager.Instance.SpawnFakeClue();
        TurnManager.Instance.OnBeastUsedAbility();
    }

    [Command]
    public void CmdErase()
    {
        if (eraseCooldown > 0 || !TurnManager.Instance.IsBeastTurn) return;
        eraseCooldown = 3;
        ClueManager.Instance.EraseAllClues();
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
    public void TargetNotifyTurn(NetworkConnectionToClient target, bool isFirstPlacement, int dc, int fc, int ec)
    {
        if (GameHUD.Instance != null)
            GameHUD.Instance.SetPhaseText(isFirstPlacement ? "Оберіть початкову позицію!" : "Твій хід, Звіре!");
        if (AbilityPanel.Instance != null)
            AbilityPanel.Instance.RefreshBeast(this, dc, fc, ec);
    }

    [Server]
    public void TickCooldowns()
    {
        if (dashCooldown  > 0) dashCooldown--;
        if (fakeCooldown  > 0) fakeCooldown--;
        if (eraseCooldown > 0) eraseCooldown--;
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
