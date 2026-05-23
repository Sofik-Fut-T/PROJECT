using Mirror;
using UnityEngine;

public enum GameState { Lobby, Playing, BeastWin, HuntersWin }

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncVar(hook = nameof(OnStateSync))] public GameState state = GameState.Lobby;
    [SyncVar(hook = nameof(OnRoundSync))]  public int currentRound;

    public int CurrentRound => currentRound;

    [Header("Settings")]
    public int maxRounds = 12;

    private void Awake() => Instance = this;

    // ─── Server API ──────────────────────────────────────────────────────────

    [Server]
    public void StartGame()
    {
        currentRound = 1;
        state        = GameState.Playing;
        TurnManager.Instance.BeginBeastTurn();
    }

    [Server]
    public void EndRound()
    {
        currentRound++;
        if (currentRound > maxRounds) BeastWins();
        else TurnManager.Instance.BeginBeastTurn();
    }

    [Server]
    public void HuntersWin()
    {
        state = GameState.HuntersWin;
        RpcEndScreen("Мисливці перемогли! Звіра спіймано.");
    }

    [Server]
    public void BeastWins()
    {
        state = GameState.BeastWin;
        RpcEndScreen($"Звір втік! Вижив {maxRounds} раундів.");
    }

    // ─── RPCs ────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcEndScreen(string message) =>
        GameHUD.Instance?.ShowEndScreen(message);

    // ─── Hooks ───────────────────────────────────────────────────────────────

    private void OnStateSync(GameState _, GameState next) =>
        GameHUD.Instance?.UpdateState(next);

    private void OnRoundSync(int _, int next) =>
        GameHUD.Instance?.UpdateRound(next, maxRounds);
}
