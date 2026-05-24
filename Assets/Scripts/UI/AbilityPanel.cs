using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityPanel : MonoBehaviour
{
    public static AbilityPanel Instance { get; private set; }

    [Header("Beast Abilities")]
    public GameObject      beastPanel;
    public Button          btnDash;
    public Button          btnFakeTrail;
    public Button          btnErase;
    public Button          btnSkip;
    public TextMeshProUGUI dashLabel;
    public TextMeshProUGUI fakeLabel;
    public TextMeshProUGUI eraseLabel;
    public TextMeshProUGUI skipLabel;
    public TextMeshProUGUI dashCD;
    public TextMeshProUGUI fakeCD;
    public TextMeshProUGUI eraseCD;

    [Header("Hunter Abilities")]
    public GameObject      hunterPanel;
    public Button          btnSpecial;
    public Button          btnEndTurn;
    public TextMeshProUGUI specialLabel;
    public TextMeshProUGUI endTurnLabel;
    public TextMeshProUGUI specialCD;

    private BeastController  _beast;
    private HunterController _hunter;

    private void Awake() => Instance = this;

    private void Start() => HideAll();

    public void HideAll()
    {
        beastPanel.SetActive(false);
        hunterPanel.SetActive(false);
        _beast  = null;
        _hunter = null;
    }

    public void RefreshBeast(BeastController b, int dc, int fc, int ec)
    {
        _beast = b;
        beastPanel.SetActive(true);
        hunterPanel.SetActive(false);

        btnDash.onClick.RemoveAllListeners();
        btnDash.onClick.AddListener(() =>
        {
            if (NodeInput.Instance != null)
                NodeInput.Instance.SetMode(NodeInput.InputMode.Dash);
        });

        btnFakeTrail.onClick.RemoveAllListeners();
        btnFakeTrail.onClick.AddListener(() => { if (_beast != null) _beast.CmdFakeTrail(); });

        btnErase.onClick.RemoveAllListeners();
        btnErase.onClick.AddListener(() => { if (_beast != null) _beast.CmdErase(); });

        btnSkip.onClick.RemoveAllListeners();
        btnSkip.onClick.AddListener(() => { if (_beast != null) _beast.CmdSkipAbility(); });

        if (dashLabel  != null) dashLabel.text  = "Різкий ривок";
        if (fakeLabel  != null) fakeLabel.text  = "Хибний слід";
        if (eraseLabel != null) eraseLabel.text = "Стерти сліди";
        if (skipLabel  != null) skipLabel.text  = "Пропустити хід";

        RefreshBeastCooldown(dc, 0);
        RefreshBeastCooldown(fc, 1);
        RefreshBeastCooldown(ec, 2);
    }

    public void RefreshHunter(HunterController h, int sc)
    {
        _hunter = h;
        hunterPanel.SetActive(true);
        beastPanel.SetActive(false);

        btnSpecial.onClick.RemoveAllListeners();
        btnSpecial.onClick.AddListener(() =>
        {
            if (NodeInput.Instance != null)
                NodeInput.Instance.SetMode(NodeInput.InputMode.SpecialAbility);
        });

        btnEndTurn.onClick.RemoveAllListeners();
        btnEndTurn.onClick.AddListener(() => { if (_hunter != null) _hunter.CmdEndTurn(); });

        if (specialLabel != null)
            specialLabel.text = h.role switch
            {
                HunterRole.Tracker => "Точна підказка",
                HunterRole.Scout   => "Зайвий крок",
                HunterRole.Archer  => "Влучний постріл",
                _                  => "Спец. здібність"
            };
        if (endTurnLabel != null) endTurnLabel.text = "Завершити хід";

        RefreshHunterCooldown(sc);
    }

    // Called by SyncVar hooks on BeastController — slot: 0=Dash, 1=Fake, 2=Erase
    public void RefreshBeastCooldown(int cd, int slot)
    {
        string label = cd > 0 ? $"Через {TurnsText(cd)}" : "Готово";
        switch (slot)
        {
            case 0: dashCD.text  = label; btnDash.interactable      = cd == 0; break;
            case 1: fakeCD.text  = label; btnFakeTrail.interactable = cd == 0; break;
            case 2: eraseCD.text = label; btnErase.interactable     = cd == 0; break;
        }
    }

    // Called by SyncVar hook on HunterController
    public void RefreshHunterCooldown(int cd)
    {
        specialCD.text          = cd > 0 ? $"Через {TurnsText(cd)}" : "Готово";
        btnSpecial.interactable = cd == 0;
    }

    private static string TurnsText(int n) =>
        n == 1 ? "1 хід" : n < 5 ? $"{n} ходи" : $"{n} ходів";
}
