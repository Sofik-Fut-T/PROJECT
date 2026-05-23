using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Info Bar")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI phaseText;

    [Header("End Screen")]
    public GameObject      endScreenPanel;
    public TextMeshProUGUI endScreenText;

    [Header("Notification")]
    public TextMeshProUGUI notificationText;
    private float          notifTimer;

    private void Awake()
    {
        Instance = this;
        endScreenPanel.SetActive(false);
    }

    private void Update()
    {
        if (notifTimer > 0)
        {
            notifTimer -= Time.deltaTime;
            if (notifTimer <= 0)
            {
                notificationText.text = "";
            }
        }
    }

    public void UpdateRound(int current, int max) =>
        roundText.text = $"Раунд  {current} / {max}";

    public void SetPhaseText(string text) =>
        phaseText.text = text;

    public void UpdatePhase(GamePhase phase)
    {
        // HunterTurn text is set by RpcPhaseAnnounce (includes nickname) — don't overwrite it here.
        // OnPhaseSync fires from a SyncVar batch that arrives AFTER the RPC on remote clients.
        if (phase == GamePhase.HunterTurn) { return; }
        phaseText.text = phase switch
        {
            GamePhase.BeastTurn => "Хід Звіра",
            GamePhase.CluePhase => "Підказка...",
            _                   => ""
        };
    }

    public void UpdateState(GameState _) { }

    public void ShowEndScreen(string message)
    {
        endScreenPanel.SetActive(true);
        endScreenText.text = message;
    }

    public void ShowCheckResult(int nodeId, bool found)
    {
        string msg = found
            ? "Знайдено! Звір спійманий!"
            : $"Нода {nodeId} — порожньо.";
        ShowNotification(msg);
    }

    public void ShowNotification(string text, float duration = 2.5f)
    {
        notificationText.text = text;
        notifTimer = duration;
    }
}
