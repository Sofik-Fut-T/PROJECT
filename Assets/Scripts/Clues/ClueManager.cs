using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ClueManager : NetworkBehaviour
{
    public static ClueManager Instance { get; private set; }

    [Header("Light Effect")]
    public GameObject lightPrefab;
    public float      lightYOffset = 1.5f;

    private readonly List<ClueData> _history = new();

    // Server-side: how many remaining rounds to suppress the light after Erase
    private int _lightSkipRounds;

    // Client-side: the locally instantiated light object
    private GameObject _currentLight;

    private void Awake() => Instance = this;

    // ─── Called by TurnManager ────────────────────────────────────────────────

    [Server]
    public void GenerateAndSend(int beastNodeId, int round)
    {
        MapNode node = GridManager.Instance.GetNode(beastNodeId);
        string zone = (node != null && !string.IsNullOrEmpty(node.zone)) ? node.zone : null;

        var clue = new ClueData
        {
            type     = ClueType.Zone,
            round    = round,
            zoneName = zone,
            isFake   = false
        };

        _history.Add(clue);
        RpcReceiveClue(clue);

        // Spawn directional light from round 6 onward
        if (round >= 4)
        {
            if (_lightSkipRounds > 0)
            {
                _lightSkipRounds--;
                RpcDespawnLight();
            }
            else
            {
                Vector3 beastPos = GridManager.Instance.NodeToWorld(beastNodeId);
                float radius = round < 12
                    ? 15f
                    : Mathf.Lerp(12f, 1f, (round - 12f) / 3f);

                Vector3 lightPos = new Vector3(
                    beastPos.x + Random.Range(-radius, radius),
                    beastPos.y + lightYOffset,
                    beastPos.z + Random.Range(-radius, radius));

                RpcSpawnLight(lightPos);
            }
        }
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
    public void EraseAllClues()
    {
        _history.Clear();
        _lightSkipRounds = 2;
        RpcClearAll();
        RpcDespawnLight();
    }

    [Server]
    public void SendPreciseClue(NetworkConnectionToClient target, int round)
    {
        var beast = FindFirstObjectByType<BeastController>();
        if (beast == null) return;

        MapNode node = GridManager.Instance.GetNode(beast.ServerNodeId);
        string zone = (node != null && !string.IsNullOrEmpty(node.zone)) ? node.zone : null;

        var clue = new ClueData
        {
            type     = ClueType.Zone,
            round    = round,
            zoneName = zone,
            isFake   = false
        };
        TargetPreciseClue(target, clue);
    }

    // ─── RPCs ─────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcReceiveClue(ClueData clue) => ClueLogPanel.Instance?.AddEntry(clue);

    [ClientRpc]
    private void RpcClearAll() => ClueLogPanel.Instance?.ClearAll();

    [ClientRpc]
    private void RpcSpawnLight(Vector3 pos)
    {
        if (_currentLight != null) Destroy(_currentLight);
        if (lightPrefab != null)
            _currentLight = Instantiate(lightPrefab, pos, Quaternion.identity);
    }

    [ClientRpc]
    private void RpcDespawnLight()
    {
        if (_currentLight != null)
        {
            Destroy(_currentLight);
            _currentLight = null;
        }
    }

    [TargetRpc]
    private void TargetPreciseClue(NetworkConnectionToClient target, ClueData clue) =>
        ClueLogPanel.Instance?.AddEntry(clue);
}
