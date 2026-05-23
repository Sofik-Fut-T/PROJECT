using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles the first screen: nickname + host/join.
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject menuPanel;

    [Header("Fields")]
    public TMP_InputField nicknameInput;
    public TMP_InputField joinAddressInput;   // IP to join
    public TextMeshProUGUI localIPText;       // shows host's LAN IP

    [Header("Buttons")]
    public Button hostButton;
    public Button joinButton;

    [Header("Status")]
    public TextMeshProUGUI statusText;

    private HuntNetworkManager _net;

    private void Awake() => Instance = this;

    private void Start()
    {
        _net = FindObjectOfType<HuntNetworkManager>();

        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);

        // Pre-fill nickname from last session
        nicknameInput.text = PlayerPrefs.GetString("nickname", "Player");

        // Show local IP so the host can tell others
        localIPText.text = $"Твій IP:  {GetLocalIP()}";

        if (_net == null)
        {
            Debug.LogError("[MenuUI] HuntNetworkManager не знайдений у сцені! " +
                           "Додай GameObject з компонентом HuntNetworkManager.");
        }
    }

    // ─── Button handlers ─────────────────────────────────────────────────────

    private void OnHost()
    {
        SaveNickname();
        statusText.text = "Запуск хосту...";
        _net.StartHost();
        menuPanel.SetActive(false);
        LobbyUI.Instance?.ShowLobby(isHost: true);
    }

    private void OnJoin()
    {
        SaveNickname();
        string addr = joinAddressInput.text.Trim();
        _net.networkAddress = string.IsNullOrEmpty(addr) ? "localhost" : addr;
        statusText.text = $"Підключення до {_net.networkAddress}...";
        _net.StartClient();

        // LobbyUI.ShowLobby will be called by HuntNetworkManager.OnClientConnect
    }

    // Called by HuntNetworkManager when client successfully connects
    public void OnConnectedAsClient()
    {
        menuPanel.SetActive(false);
        LobbyUI.Instance?.ShowLobby(isHost: false);
    }

    public void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    public void OnConnectionFailed()
    {
        SetStatus("Не вдалось підключитись. Перевір IP.");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SaveNickname()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) nick = "Player";
        PlayerPrefs.SetString("nickname", nick);
    }

    private static string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { /* ignore */ }
        return "127.0.0.1";
    }
}
