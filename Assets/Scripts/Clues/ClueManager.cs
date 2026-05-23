using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ClueManager : NetworkBehaviour
{
    public static ClueManager Instance { get; private set; }

    private readonly List<ClueData> _history = new();

    private void Awake() => Instance = this;

    // ─── Called by TurnManager ────────────────────────────────────────────────

    [Server]
    public void GenerateAndSend(int beastNodeId, int round)
    {
        var ng  = GridManager.Instance;
        Vector3 pos = ng.NodeToWorld(beastNodeId);

        ClueType[] pool = { ClueType.SameRow, ClueType.SameColumn, ClueType.Zone };
        var type = pool[Random.Range(0, pool.Length)];

        var clue = new ClueData
        {
            type         = type,
            round        = round,
            lineValue    = type == ClueType.SameRow ? pos.z : pos.x,
            centerNodeId = beastNodeId,
            zoneRadius   = 2.5f,
            isFake       = false
        };

        _history.Add(clue);
        RpcReceiveClue(clue);
    }

    [Server]
    public void SpawnFakeClue()
    {
        var fake = new ClueData
        {
            type   = ClueType.Fake,
            round  = GameManager.Instance.CurrentRound,
            isFake = true
        };
        _history.Add(fake);
        RpcReceiveClue(fake);
    }

    [Server]
    public void EraseLastClue()
    {
        for (int i = _history.Count - 1; i >= 0; i--)
        {
            if (!_history[i].isFake)
            {
                int round = _history[i].round;
                _history.RemoveAt(i);
                RpcEraseClue(round);
                return;
            }
        }
    }

    [Server]
    public void SendPreciseClue(NetworkConnectionToClient target, int round)
    {
        var beast = FindFirstObjectByType<BeastController>();
        if (beast == null) return;

        var clue = new ClueData
        {
            type         = ClueType.Zone,
            round        = round,
            centerNodeId = beast.ServerNodeId,
            zoneRadius   = 0f,   // exact node only
            isFake       = false
        };
        TargetPreciseClue(target, clue);
    }

    // ─── RPCs ─────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcReceiveClue(ClueData clue)
    {
        ClueLogPanel.Instance?.AddEntry(clue);
        HighlightClue(clue);
    }

    [ClientRpc]
    private void RpcEraseClue(int round)
    {
        ClueLogPanel.Instance?.RemoveEntry(round);
        GridManager.Instance?.ClearHighlights();
    }

    [TargetRpc]
    private void TargetPreciseClue(NetworkConnectionToClient target, ClueData clue)
    {
        ClueLogPanel.Instance?.AddEntry(clue);
        GridManager.Instance?.HighlightNode(clue.centerNodeId, CellHighlight.Clue);
    }

    // ─── Highlight helpers ────────────────────────────────────────────────────

    private static void HighlightClue(ClueData clue)
    {
        var ng = GridManager.Instance;
        if (ng == null) return;

        List<int> nodes;
        switch (clue.type)
        {
            case ClueType.SameRow:
                nodes = ng.GetNodesInRow(clue.lineValue);
                break;
            case ClueType.SameColumn:
                nodes = ng.GetNodesInColumn(clue.lineValue);
                break;
            case ClueType.Zone:
                nodes = ng.GetNodesInZone(clue.centerNodeId, clue.zoneRadius);
                break;
            default:
                return;
        }
        ng.HighlightNodes(nodes, CellHighlight.Clue);
    }
}
