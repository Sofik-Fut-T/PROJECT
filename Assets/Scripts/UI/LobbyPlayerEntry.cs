using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row in the lobby player list.
public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI roleText;
    public Image           background;

    [Header("Colors")]
    public Color selfColor  = new Color(0.2f, 0.5f, 1f, 0.3f);
    public Color otherColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);

    public void Setup(NetworkPlayer player, bool isLocalPlayer)
    {
        nicknameText.text = player.nickname;

        roleText.text = player.role switch
        {
            PlayerRole.Beast   => "Звір",
            PlayerRole.Tracker => "Слідопит",
            PlayerRole.Scout   => "Розвідник",
            PlayerRole.Archer  => "Стрілець",
            _                  => "— не вибрано —"
        };

        roleText.color = player.role == PlayerRole.Unassigned
            ? new Color(1f, 0.4f, 0.4f)
            : Color.white;

        if (background != null)
            background.color = isLocalPlayer ? selfColor : otherColor;
    }
}
