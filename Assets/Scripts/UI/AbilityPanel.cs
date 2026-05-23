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
    public TextMeshProUGUI dashCD;
    public TextMeshProUGUI fakeCD;
    public TextMeshProUGUI eraseCD;

    [Header("Hunter Abilities")]
    public GameObject      hunterPanel;
    public Button          btnSpecial;
    public Button          btnEndTurn;
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

    public void RefreshBeast(BeastController b)
    {
        _beast = b;
        beastPanel.SetActive(true);
        hunterPanel.SetActive(false);

        btnDash.onClick.RemoveAllListeners();
        btnDash.onClick.AddListener(() =>
        {
            // Dash needs a target node — next click selects it via Move mode
            // CmdDash is called manually; for now treat as normal move
            if (NodeInput.Instance != null)
            {
                NodeInput.Instance.SetMode(NodeInput.InputMode.Move);
            }
        });

        btnFakeTrail.onClick.RemoveAllListeners();
        btnFakeTrail.onClick.AddListener(() => _beast?.CmdFakeTrail());

        btnErase.onClick.RemoveAllListeners();
        btnErase.onClick.AddListener(() => _beast?.CmdErase());

        btnSkip.onClick.RemoveAllListeners();
        btnSkip.onClick.AddListener(() => _beast?.CmdSkipAbility());
    }

    public void RefreshHunter(HunterController h)
    {
        _hunter = h;
        hunterPanel.SetActive(true);
        beastPanel.SetActive(false);

        btnSpecial.onClick.RemoveAllListeners();
        btnSpecial.onClick.AddListener(() =>
        {
            if (NodeInput.Instance != null)
            {
                NodeInput.Instance.SetMode(NodeInput.InputMode.SpecialAbility);
            }
        });

        btnEndTurn.onClick.RemoveAllListeners();
        btnEndTurn.onClick.AddListener(() => _hunter?.CmdEndTurn());
    }

    private void Update()
    {
        if (_beast != null)
        {
            dashCD.text  = _beast.dashCooldown  > 0 ? $"CD {_beast.dashCooldown}"  : "Готово";
            fakeCD.text  = _beast.fakeCooldown  > 0 ? $"CD {_beast.fakeCooldown}"  : "Готово";
            eraseCD.text = _beast.eraseCooldown > 0 ? $"CD {_beast.eraseCooldown}" : "Готово";

            btnDash.interactable      = _beast.dashCooldown  == 0;
            btnFakeTrail.interactable = _beast.fakeCooldown  == 0;
            btnErase.interactable     = _beast.eraseCooldown == 0;
        }
        if (_hunter != null)
        {
            specialCD.text = _hunter.specialCooldown > 0
                ? $"CD {_hunter.specialCooldown}"
                : "Готово";

            btnSpecial.interactable = _hunter.specialCooldown == 0;
        }
    }
}
