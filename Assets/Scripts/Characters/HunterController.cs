using Mirror;
using UnityEngine;

public enum HunterRole { Tracker, Scout, Archer }

public class HunterController : CharacterBase
{
    [SyncVar(hook = nameof(OnNodeSync))] public int currentNodeId = -1;

    [SyncVar] public HunterRole role;
    [SyncVar] public int        hunterIndex;
    [SyncVar] public int        specialCooldown;

    [SerializeField] private Transform  currentNodeTransform;
    [SerializeField] private GameObject localPlayerIndicatorPrefab;

    private int  _specialCD;
    private bool _movedThisTurn;
    private bool _actedThisTurn;

    // Called when this client gains authority over this object (i.e. this is the hunter player)
    public override void OnStartAuthority()
    {
        NodeInput nodeInput = FindFirstObjectByType<NodeInput>();
        if (nodeInput != null) nodeInput.Init(null, this);

        if (localPlayerIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(localPlayerIndicatorPrefab, transform);
            indicator.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.15f, 0f), Quaternion.identity);
        }
    }

    // ─── Commands ────────────────────────────────────────────────────────────

    [Command]
    public void CmdMove(int targetNodeId)
    {
        bool isTurn    = TurnManager.Instance.IsHunterTurn(hunterIndex);
        bool neighbors = IsNeighbor(currentNodeId, targetNodeId);
        Debug.Log($"[Hunter.CmdMove] target={targetNodeId} from={currentNodeId} " +
                  $"hunterIndex={hunterIndex} isTurn={isTurn} moved={_movedThisTurn} neighbor={neighbors}");

        if (!isTurn)    { Debug.Log("[Hunter.CmdMove] Відхилено — не хід цього мисливця"); return; }
        if (_movedThisTurn) { Debug.Log("[Hunter.CmdMove] Відхилено — вже ходив"); return; }
        if (!neighbors) { Debug.Log("[Hunter.CmdMove] Відхилено — не сусідня нода"); return; }

        SetNodeId(targetNodeId);
        _movedThisTurn = true;

        if (IsBeastHere(targetNodeId)) { GameManager.Instance.HuntersWin(); return; }
        // Turn does not end here — player must press End Turn.
    }

    [Command]
    public void CmdCheckCell(int targetNodeId)
    {
        if (!TurnManager.Instance.IsHunterTurn(hunterIndex)) return;
        if (_actedThisTurn) return;

        _actedThisTurn = true;
        bool hit = IsBeastHere(targetNodeId);
        TargetCheckResult(connectionToClient, targetNodeId, hit);

        if (hit) GameManager.Instance.HuntersWin();
        // Miss — player must press End Turn.
    }

    [Command]
    public void CmdUseSpecial(int targetNodeId)
    {
        if (!TurnManager.Instance.IsHunterTurn(hunterIndex)) return;
        if (_actedThisTurn || _specialCD > 0) return;

        _actedThisTurn = true;
        _specialCD = 3; specialCooldown = 3;

        switch (role)
        {
            case HunterRole.Tracker:
                ClueManager.Instance.SendPreciseClue(connectionToClient,
                    GameManager.Instance.CurrentRound);
                break;
            case HunterRole.Scout:
                _movedThisTurn = false; // extra move
                _actedThisTurn = false;
                _specialCD = 3; specialCooldown = 3;
                return;
            case HunterRole.Archer:
                bool hit = IsBeastHere(targetNodeId);
                TargetCheckResult(connectionToClient, targetNodeId, hit);
                if (hit) { GameManager.Instance.HuntersWin(); return; }
                break;
        }
        // Player must press End Turn.
    }

    [Command]
    public void CmdEndTurn()
    {
        if (!TurnManager.Instance.IsHunterTurn(hunterIndex)) return;
        TurnManager.Instance.OnHunterDone();
    }

    // ─── Server helpers ───────────────────────────────────────────────────────

    [Server]
    public void BeginTurn()
    {
        _movedThisTurn = false;
        _actedThisTurn = false;
        if (_specialCD > 0) { _specialCD--; specialCooldown = _specialCD; }
        TargetNotifyTurn(connectionToClient);
    }

    [Server]
    private bool IsBeastHere(int nodeId)
    {
        var beast = FindFirstObjectByType<BeastController>();
        return beast != null && beast.ServerNodeId == nodeId;
    }

    [TargetRpc]
    private void TargetNotifyTurn(NetworkConnectionToClient target)
    {
        GameHUD.Instance?.SetPhaseText("Твій хід, Мисливцю!");
        AbilityPanel.Instance?.RefreshHunter(this);
    }

    [TargetRpc]
    private void TargetCheckResult(NetworkConnectionToClient target, int nodeId, bool found)
    {
        GridManager.Instance?.HighlightNode(nodeId,
            found ? CellHighlight.Confirmed : CellHighlight.Suspected);
        GameHUD.Instance?.ShowCheckResult(nodeId, found);
    }

    // Called on server — keeps server transform in sync so NetworkTransform and IsBeastHere work correctly
    [Server]
    private void SetNodeId(int nodeId)
    {
        currentNodeId = nodeId;                              // SyncVar → triggers OnNodeSync on clients
        MapNode node  = GridManager.Instance.GetNode(nodeId);
        currentNodeTransform = node != null ? node.transform : null;
        if (node != null)
        {
            transform.position = node.transform.position;   // server-side position for NetworkTransform sync
            RpcSmoothMoveTo(node.transform.position);        // smooth visual on all clients
        }
    }

    // OnNodeSync fires on CLIENTS only — used for initial spawn position (before first SetNodeId call)
    private void OnNodeSync(int _, int newId)
    {
        if (GridManager.Instance == null) return;
        MapNode node = GridManager.Instance.GetNode(newId);
        if (node != null) transform.position = node.transform.position;
        currentNodeTransform = node != null ? node.transform : null;
    }
}
