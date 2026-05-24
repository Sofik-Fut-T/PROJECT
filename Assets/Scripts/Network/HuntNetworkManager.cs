using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HuntNetworkManager : NetworkManager
{
    [Header("Character Prefabs")]
    public GameObject beastPrefab;
    public GameObject hunterTrackerPrefab;
    public GameObject hunterScoutPrefab;
    public GameObject hunterArcherPrefab;

    [Header("Scenes")]
    public string gameSceneName = "GameScene";
    public string menuSceneName = "MenuScene";

    [Header("Game Settings")]
    public int maxRounds = 15;

    private readonly List<NetworkPlayer> _players = new();

    // static — виживає між сценами разом з NetworkManager
    private static readonly Dictionary<int, (PlayerRole role, int hunterIndex)> _roleCache = new();
    private static int _expectedPlayerCount = 0;

    // ─── Connection / player events ──────────────────────────────────────────

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        var np = conn.identity.GetComponent<NetworkPlayer>();
        _players.Add(np);

        if (_roleCache.TryGetValue(conn.connectionId, out var cached))
        {
            // Відновлюємо ролі після переходу на GameScene
            np.role        = cached.role;
            np.hunterIndex = cached.hunterIndex;
            _roleCache.Remove(conn.connectionId);
        }
        else
        {
            // Перше підключення — гравець обере роль самостійно в лобі
            np.role        = PlayerRole.Unassigned;
            np.hunterIndex = -1;
        }

        Debug.Log($"[Server] Player conn={conn.connectionId} → {np.role}");
        LobbyUI.Instance?.RefreshList();

    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var np = conn.identity?.GetComponent<NetworkPlayer>();

        bool inGame = SceneManager.GetActiveScene().name == gameSceneName
                      && GameManager.Instance != null
                      && GameManager.Instance.state == GameState.Playing;

        if (inGame && np != null)
            HandleGameDisconnect(np);

        if (np != null) _players.Remove(np);
        base.OnServerDisconnect(conn);
        if (!inGame) LobbyUI.Instance?.RefreshList();
    }

    private void HandleGameDisconnect(NetworkPlayer np)
    {
        if (np.role == PlayerRole.Beast)
        {
            GameManager.Instance.EndGameDisconnect($"{np.nickname} (Звір) відключився.");
            StartCoroutine(ReturnToMenuDelayed());
            return;
        }

        if (!np.role.IsHunter()) return;

        int remaining = _players.Count(p => p != np && p.role.IsHunter());

        if (remaining == 0)
        {
            GameManager.Instance.EndGameDisconnect("Всі мисливці відключились.");
            StartCoroutine(ReturnToMenuDelayed());
        }
        else
        {
            GameManager.Instance.NotifyDisconnect($"{np.nickname} відключився. Продовжуємо без нього.");
            TurnManager.Instance?.MarkHunterDisconnected(np.hunterIndex);
        }
    }

    private IEnumerator ReturnToMenuDelayed()
    {
        yield return new WaitForSeconds(3.5f);
        ServerChangeScene(menuSceneName);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        if (!NetworkServer.active && MenuUI.Instance != null)
            MenuUI.Instance.OnConnectedAsClient();
    }

    public override void OnClientDisconnect()
    {
        bool inGame = SceneManager.GetActiveScene().name == gameSceneName;
        base.OnClientDisconnect();

        if (inGame)
        {
            if (GameHUD.Instance != null) { GameHUD.Instance.ShowEndScreen("З'єднання перервано. Повернення в меню..."); }
            StartCoroutine(ReturnToMenuLocal());
        }
        else
        {
            if (MenuUI.Instance != null) { MenuUI.Instance.OnConnectionFailed(); }
        }
    }

    private static readonly WaitForSeconds WaitReturn = new(3.5f);

    private IEnumerator ReturnToMenuLocal()
    {
        yield return WaitReturn;
        SceneManager.LoadScene(menuSceneName);
    }

    // Викликається на КЛІЄНТАХ після завантаження нової сцени
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        bool inGame = SceneManager.GetActiveScene().name == gameSceneName;
        if (!inGame) return;

        // Показати ігровий HUD — GameHUD знаходиться в GameScene
        if (GameHUD.Instance != null)
            GameHUD.Instance.gameObject.SetActive(true);
    }

    // ─── Start game → перехід на GameScene ──────────────────────────────────

    public void StartGame()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[HuntNetworkManager] Тільки хост може запустити гру.");
            return;
        }
        if (_players.Count < 2)
        {
            Debug.LogWarning("[HuntNetworkManager] Потрібно мінімум 2 гравці.");
            return;
        }

        // Зберігаємо ролі до переходу — NetworkPlayer буде перестворено в новій сцені
        _roleCache.Clear();
        foreach (var p in _players)
            _roleCache[p.connectionToClient.connectionId] = (p.role, p.hunterIndex);

        _expectedPlayerCount = _players.Count;
        _players.Clear(); // буде заповнено знову в OnServerAddPlayer після переходу

        ServerChangeScene(gameSceneName); // Mirror переводить ВСІХ гравців
    }

    // ─── Запуск гри після завантаження сцени ─────────────────────────────────

    // Fires on server after the server itself has loaded the new scene
    public override void OnServerSceneChanged(string newSceneName)
    {
        base.OnServerSceneChanged(newSceneName);
        if (newSceneName == gameSceneName && _expectedPlayerCount > 0)
            StartCoroutine(LaunchGame());
    }

    private IEnumerator LaunchGame()
    {
        Debug.Log($"[LaunchGame] Чекаємо {_expectedPlayerCount} гравців...");

        // 1) Wait until every expected player has called AddPlayer (OnServerAddPlayer)
        float timeout = 30f;
        while (_players.Count < _expectedPlayerCount && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (_players.Count < _expectedPlayerCount)
            Debug.LogWarning($"[LaunchGame] Таймаут! Підключено {_players.Count}/{_expectedPlayerCount}");

        // 2) Wait until every connection is marked as ready
        timeout = 10f;
        while (timeout > 0f)
        {
            bool allReady = true;
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (!conn.isReady) { allReady = false; break; }
            }
            if (allReady) break;
            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[LaunchGame] Всі підключені. _players.Count={_players.Count}");
        foreach (NetworkPlayer p in _players)
            Debug.Log($"  → conn={p.connectionToClient?.connectionId} role={p.role}");

        _expectedPlayerCount = 0;

        GameManager  gameMgr = GameManager.Instance;
        TurnManager  turnMgr = TurnManager.Instance;
        ClueManager  clueMgr = ClueManager.Instance;
        GridManager  gridMgr = GridManager.Instance;

        Debug.Log($"[LaunchGame] GameManager={gameMgr != null}  TurnManager={turnMgr != null}  " +
                  $"ClueManager={clueMgr != null}  GridManager={gridMgr != null}");

        if (gameMgr == null || turnMgr == null || clueMgr == null || gridMgr == null)
        {
            Debug.LogError("[LaunchGame] ПОМИЛКА — відсутні менеджери в GameScene! " +
                           "Перевір що GameManager, TurnManager, ClueManager, GridManager є в GameScene.");
            yield break;
        }

        Debug.Log($"[LaunchGame] GridManager знайшов {gridMgr.NodeCount} нодів.");

        gameMgr.maxRounds = maxRounds;
        SpawnCharacters();
        gameMgr.StartGame();

        Debug.Log("[LaunchGame] Гру запущено.");
    }

    // ─── Спавн персонажів ────────────────────────────────────────────────────

    private void SpawnCharacters()
    {
        GridManager gm2 = GridManager.Instance;

        if (gm2 == null)
        {
            Debug.LogError("[HuntNetworkManager] GridManager not found in GameScene!");
            return;
        }

        // Log all players for diagnosis
        Debug.Log($"[SpawnCharacters] _players.Count={_players.Count}");
        foreach (var p in _players)
            Debug.Log($"  conn={p.connectionToClient?.connectionId} role={p.role} hunterIndex={p.hunterIndex}");

        NetworkPlayer beastPlayer = _players.FirstOrDefault(p => p.role == PlayerRole.Beast);

        List<NetworkPlayer> hunterPlayers = _players
            .Where(p => p.role.IsHunter())
            .OrderBy(p => p.hunterIndex)
            .ToList();
        Debug.Log($"[SpawnCharacters] beastPlayer={(beastPlayer != null ? beastPlayer.connectionToClient?.connectionId.ToString() : "null")} hunterPlayers.Count={hunterPlayers.Count}");

        if (beastPlayer != null)
        {
            if (beastPrefab == null)
            {
                Debug.LogError("[SpawnCharacters] beastPrefab не призначений в інспекторі!");
            }
            else
            {
                // Beast chooses start node on first click — spawn off-map so it's invisible until placed
                Vector3 holdingPos = new Vector3(1000f, 0f, 0f);
                Debug.Log($"[SpawnCharacters] Спавн Звіра (holding pos) для conn={beastPlayer.connectionToClient?.connectionId} role={beastPlayer.role}");
                GameObject beastGO = Instantiate(beastPrefab, holdingPos, Quaternion.identity);
                NetworkServer.Spawn(beastGO, beastPlayer.connectionToClient);
                // _serverNodeId remains -1; first CmdMove will place the beast
            }
        }
        else
        {
            Debug.LogError("[SpawnCharacters] Список гравців порожній — не можна заспавнити Звіра!");
        }

        // Pick unique random start nodes for each hunter
        HashSet<int> usedNodes = new();
        for (int i = 0; i < hunterPlayers.Count; i++)
        {
            NetworkPlayer hp = hunterPlayers[i];
            GameObject prefab = hp.role switch
            {
                PlayerRole.Tracker => hunterTrackerPrefab,
                PlayerRole.Scout   => hunterScoutPrefab,
                PlayerRole.Archer  => hunterArcherPrefab,
                _                  => hunterTrackerPrefab
            };
            if (prefab == null)
                prefab = hunterTrackerPrefab != null ? hunterTrackerPrefab
                       : hunterScoutPrefab   != null ? hunterScoutPrefab
                       : hunterArcherPrefab;
            if (prefab == null)
            {
                Debug.LogError($"[SpawnCharacters] Жоден Hunter prefab не призначений в інспекторі!");
                continue;
            }

            int nodeId = gm2.GetRandomNodeId();
            int attempts = 0;
            while (usedNodes.Contains(nodeId) && attempts < 50) { nodeId = gm2.GetRandomNodeId(); attempts++; }
            usedNodes.Add(nodeId);

            Vector3 spawnPos = gm2.NodeToWorld(nodeId);
            Debug.Log($"[SpawnCharacters] Спавн Мисливця {i} на ноді {nodeId} pos={spawnPos}");

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

            if (!go.TryGetComponent(out HunterController ctrl))
            {
                Debug.LogError($"[SpawnCharacters] Prefab '{prefab.name}' не має HunterController!");
                Destroy(go);
                continue;
            }

            // Set SyncVars BEFORE NetworkServer.Spawn so they're included in the initial spawn message,
            // not sent as a late delta that can arrive after the first command.
            ctrl.role = hp.role switch
            {
                PlayerRole.Scout  => HunterRole.Scout,
                PlayerRole.Archer => HunterRole.Archer,
                _                 => HunterRole.Tracker
            };
            ctrl.hunterIndex   = i;
            ctrl.currentNodeId = nodeId;

            Debug.Log($"[SpawnCharacters] currentNodeId={ctrl.currentNodeId} (перед spawn)");
            NetworkServer.Spawn(go, hunterPlayers[i].connectionToClient);
            Debug.Log($"[SpawnCharacters] currentNodeId={ctrl.currentNodeId} (після spawn)");
        }
    }
}
