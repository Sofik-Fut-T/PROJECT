using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum GamePhase { BeastTurn, CluePhase, HunterTurn }

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SyncVar(hook = nameof(OnPhaseSync))] public GamePhase phase;
    [SyncVar] public int activeHunterIndex;

    public bool IsBeastTurn   => isServer && phase == GamePhase.BeastTurn;
    public bool IsCluePhase   => isServer && phase == GamePhase.CluePhase;

    public bool IsHunterTurn(int index) =>
        isServer && phase == GamePhase.HunterTurn && activeHunterIndex == index;

    private BeastController        _beast;
    private List<HunterController> _hunters = new();
    private bool _beastMoved;
    private bool _beastAbilityDone;
    private readonly HashSet<int>  _disconnectedHunters = new();

    private void Awake() => Instance = this;

    // ─── Game flow ───────────────────────────────────────────────────────────

    [Server]
    public void BeginBeastTurn()
    {
        _beast             = FindObjectOfType<BeastController>();
        _hunters           = new List<HunterController>(FindObjectsOfType<HunterController>());
        _hunters.Sort((a, b) => a.hunterIndex.CompareTo(b.hunterIndex));
        _beastMoved       = false;
        _beastAbilityDone = false;  // beast must press Skip/use ability to end their turn

        RpcHideAbilityPanel();
        phase = GamePhase.BeastTurn;
        if (_beast != null)
        {
            _beast.BeginTurn();
            _beast.TargetNotifyTurn(_beast.connectionToClient, _beast.ServerNodeId == -1,
                _beast.dashCooldown, _beast.fakeCooldown, _beast.eraseCooldown);
        }
        RpcPhaseAnnounce("Хід Звіра");
    }

    [Server]
    public void OnBeastMoved(BeastController beast)
    {
        _beastMoved = true;
        // Ability is optional — auto-advance if skipped
        if (_beastAbilityDone) FinishBeastTurn(beast);
    }

    [Server]
    public void OnBeastUsedAbility()
    {
        _beastAbilityDone = true;
        if (_beastMoved) FinishBeastTurn(_beast);
    }

    [Server]
    private void FinishBeastTurn(BeastController beast)
    {
        beast.TickCooldowns();
        phase = GamePhase.CluePhase;
        ClueManager.Instance.GenerateAndSend(beast.ServerNodeId, GameManager.Instance.CurrentRound);

        // Small delay so clients can see the clue, then start hunter turns
        Invoke(nameof(StartHunterTurns), 1.2f);
    }

    [Server]
    private void StartHunterTurns()
    {
        activeHunterIndex = 0;
        phase = GamePhase.HunterTurn;
        AdvanceHunterTurn();
    }

    [Server]
    public void OnHunterDone()
    {
        activeHunterIndex++;
        AdvanceHunterTurn();
    }

    [Server]
    private void AdvanceHunterTurn()
    {
        // Skip over any disconnected hunters
        while (activeHunterIndex < _hunters.Count && _disconnectedHunters.Contains(activeHunterIndex))
            activeHunterIndex++;

        if (activeHunterIndex >= _hunters.Count)
        {
            GameManager.Instance.EndRound();
            return;
        }
        RpcHideAbilityPanel();
        _hunters[activeHunterIndex].BeginTurn();
        string name = LookupHunterNickname(activeHunterIndex);
        RpcPhaseAnnounce($"Хід Мисливця: {name}");
    }

    [Server]
    public void MarkHunterDisconnected(int hunterIndex)
    {
        _disconnectedHunters.Add(hunterIndex);

        // If it's currently this hunter's turn, skip it immediately
        if (phase == GamePhase.HunterTurn && activeHunterIndex == hunterIndex)
            OnHunterDone();
    }

    [Server]
    private string LookupHunterNickname(int index)
    {
        HunterController hunter = _hunters.Count > index ? _hunters[index] : null;
        if (hunter == null) return $"#{index + 1}";

        NetworkPlayer[] allPlayers = FindObjectsOfType<NetworkPlayer>();
        Debug.Log($"[TurnManager] LookupNickname index={index} conn={hunter.connectionToClient?.connectionId} allPlayers={allPlayers.Length}");

        foreach (NetworkPlayer np in allPlayers)
            if (np.connectionToClient == hunter.connectionToClient)
                return np.nickname;

        return $"#{index + 1}";
    }

    // ─── RPCs ────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcHideAbilityPanel() => AbilityPanel.Instance?.HideAll();

    [ClientRpc]
    private void RpcPhaseAnnounce(string text) =>
        GameHUD.Instance?.SetPhaseText(text);

    private void OnPhaseSync(GamePhase _, GamePhase next) =>
        GameHUD.Instance?.UpdatePhase(next);
}
