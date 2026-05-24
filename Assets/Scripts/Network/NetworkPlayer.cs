using Mirror;
using UnityEngine;

public enum PlayerRole { Unassigned, Beast, Tracker, Scout, Archer }

public static class PlayerRoleExt
{
    public static bool IsHunter(this PlayerRole r) =>
        r == PlayerRole.Tracker || r == PlayerRole.Scout || r == PlayerRole.Archer;
}

// One NetworkPlayer per connected client. Lives for the whole session.
public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local { get; private set; }

    [SyncVar(hook = nameof(OnNicknameSync))] public string nickname   = "Player";
    [SyncVar(hook = nameof(OnRoleSync))]     public PlayerRole role   = PlayerRole.Unassigned;
    [SyncVar(hook = nameof(OnReadySync))]    public bool isReady;
    [SyncVar]                                public int  hunterIndex  = -1;

    // ─── Local player init ───────────────────────────────────────────────────

    public override void OnStartLocalPlayer()
    {
        Local = this;
        string saved = PlayerPrefs.GetString("nickname", $"Player{netId}");
        CmdSetNickname(saved);
    }

    // ─── Commands (client → server) ─────────────────────────────────────────

    [Command]
    public void CmdSetNickname(string name)
    {
        nickname = name.Length > 16 ? name[..16] : name;
    }

    [Command]
    public void CmdChooseRole(PlayerRole chosen)
    {
        if (chosen == PlayerRole.Unassigned)
        {
            role        = PlayerRole.Unassigned;
            hunterIndex = -1;
            return;
        }

        // Every specific role is exclusive — check if someone else already holds it
        var all = FindObjectsOfType<NetworkPlayer>();
        foreach (var p in all)
            if (p != this && p.role == chosen) return;

        if (chosen.IsHunter())
        {
            int nextIndex = 0;
            foreach (var p in all)
                if (p != this && p.role.IsHunter() && p.hunterIndex >= nextIndex)
                    nextIndex = p.hunterIndex + 1;
            hunterIndex = nextIndex;
        }
        else
        {
            hunterIndex = -1;
        }

        role = chosen;
    }

    [Command]
    public void CmdSetReady(bool ready) => isReady = ready;

    // ─── SyncVar hooks (run on all clients) ─────────────────────────────────

    private void OnNicknameSync(string _, string __) => LobbyUI.Instance?.RefreshList();
    private void OnRoleSync(PlayerRole _, PlayerRole __)    => LobbyUI.Instance?.RefreshList();
    private void OnReadySync(bool _, bool __)               => LobbyUI.Instance?.RefreshList();
}
