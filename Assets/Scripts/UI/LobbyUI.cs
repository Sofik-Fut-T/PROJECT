using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject lobbyPanel;

    [Header("Player List")]
    public Transform  playerListParent;
    public GameObject playerEntryPrefab;

    [Header("Role Buttons")]
    public Button btnBeast;
    public Button btnTracker;
    public Button btnScout;
    public Button btnArcher;

    [Header("Host Controls")]
    public Button          startButton;
    public TextMeshProUGUI startHint;

    private bool _isHost;
    private readonly List<GameObject> _entries = new();

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake() => Instance = this;

    private void Start()
    {
        if (lobbyPanel != null)  lobbyPanel.SetActive(false);
        if (startButton != null) startButton.gameObject.SetActive(false);

        btnBeast?.onClick.AddListener(   () => ChooseRole(PlayerRole.Beast));
        btnTracker?.onClick.AddListener( () => ChooseRole(PlayerRole.Hunter));
        btnScout?.onClick.AddListener(   () => ChooseRole(PlayerRole.Hunter));
        btnArcher?.onClick.AddListener(  () => ChooseRole(PlayerRole.Hunter));
        startButton?.onClick.AddListener(OnStartGame);
    }

    public void ShowLobby(bool isHost)
    {
        _isHost = isHost;
        if (lobbyPanel != null)  lobbyPanel.SetActive(true);
        if (startButton != null) startButton.gameObject.SetActive(isHost);
        if (startHint != null)
            startHint.text = isHost ? "Очікування гравців..." : "Очікування хоста...";

        RefreshList();
    }

    // ─── Role selection ───────────────────────────────────────────────────────

    private void ChooseRole(PlayerRole role) =>
        NetworkPlayer.Local?.CmdChooseRole(role);

    // ─── Player list ─────────────────────────────────────────────────────────

    public void RefreshList()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf) return;

        if (playerEntryPrefab == null)
        {
            Debug.LogError("[LobbyUI] playerEntryPrefab not assigned!");
            return;
        }
        if (playerListParent == null)
        {
            Debug.LogError("[LobbyUI] playerListParent not assigned!");
            return;
        }

        foreach (var e in _entries)
            if (e != null) DestroyImmediate(e);
        _entries.Clear();

        for (int i = playerListParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(playerListParent.GetChild(i).gameObject);

        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            var go    = Instantiate(playerEntryPrefab, playerListParent);
            var entry = go.GetComponent<LobbyPlayerEntry>();
            if (entry == null)
            {
                Debug.LogError("[LobbyUI] LobbyPlayerEntry component missing on prefab!");
                DestroyImmediate(go);
                continue;
            }
            entry.Setup(p, p == NetworkPlayer.Local);
            _entries.Add(go);
        }

        UpdateStartButton(players);
    }

    private void UpdateStartButton(NetworkPlayer[] players)
    {
        if (!_isHost || startButton == null) return;

        bool hasBeast  = false;
        bool allPicked = players.Length >= 2;
        foreach (var p in players)
        {
            if (p.role == PlayerRole.Unassigned) allPicked = false;
            if (p.role == PlayerRole.Beast)       hasBeast  = true;
        }

        bool ready = allPicked && hasBeast;
        startButton.interactable = ready;
        if (startHint != null)
            startHint.text = ready ? "Всі готові!" : "Не всі вибрали роль...";
    }

    // ─── Start game ──────────────────────────────────────────────────────────

    private void OnStartGame()
    {
        if (!NetworkServer.active) return;
        FindFirstObjectByType<HuntNetworkManager>()?.StartGame();
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }
}
