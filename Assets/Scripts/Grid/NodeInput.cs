using UnityEngine;

public class NodeInput : MonoBehaviour
{
    public static NodeInput Instance { get; private set; }

    public enum InputMode { Move, Dash, CheckCell, SpecialAbility }
    public InputMode currentMode = InputMode.Move;

    private int _nodesLayerMask;
    private BeastController  _beast;
    private HunterController _hunter;

    private void Awake()
    {
        Instance        = this;
        _nodesLayerMask = LayerMask.GetMask("Nodes");
        Debug.Log($"[NodeInput] Awake. Nodes layer mask = {_nodesLayerMask}");
    }

    public void Init(BeastController beast, HunterController hunter)
    {
        _beast  = beast;
        _hunter = hunter;
        Debug.Log($"[NodeInput] Init — beast={beast != null} hunter={hunter != null}");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Debug.Log($"[NodeInput] Click! beast={_beast != null} hunter={_hunter != null}");

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[NodeInput] Camera.main is null!");
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, _nodesLayerMask))
        {
            Debug.Log("[NodeInput] Raycast нічого не влучив — Plate не на шарі 'Nodes'?");
            return;
        }

        Debug.Log($"[NodeInput] Raycast влучив: {hit.collider.name} (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");

        MapNode node = hit.collider.GetComponent<MapNode>()
                    ?? hit.collider.GetComponentInParent<MapNode>();

        if (node == null)
        {
            Debug.Log("[NodeInput] MapNode компонент не знайдений на об'єкті або його батьку");
            return;
        }

        if (GridManager.Instance == null)
        {
            Debug.LogError("[NodeInput] GridManager.Instance is null!");
            return;
        }

        int nodeId = GridManager.Instance.GetId(node);
        Debug.Log($"[NodeInput] MapNode знайдений, nodeId={nodeId}");

        if (nodeId < 0)
        {
            Debug.Log("[NodeInput] nodeId < 0 — нода не зареєстрована в GridManager");
            return;
        }

        HandleClick(nodeId);
    }

    private void HandleClick(int nodeId)
    {
        if (_beast != null && _beast.isOwned)
        {
            if (currentMode == InputMode.Dash)
            {
                Debug.Log($"[NodeInput] → CmdDash({nodeId}) як Звір");
                _beast.CmdDash(nodeId);
                currentMode = InputMode.Move;
            }
            else
            {
                Debug.Log($"[NodeInput] → CmdMove({nodeId}) як Звір");
                _beast.CmdMove(nodeId);
            }
            return;
        }

        if (_hunter == null || !_hunter.isOwned)
        {
            Debug.Log("[NodeInput] Немає локального персонажа — Init не викликано?");
            return;
        }

        switch (currentMode)
        {
            case InputMode.Move:
                Debug.Log($"[NodeInput] → CmdMove({nodeId}) як Мисливець (isOwned={_hunter.isOwned})");
                _hunter.CmdMove(nodeId);
                break;
            case InputMode.CheckCell:
                _hunter.CmdCheckCell(nodeId);
                currentMode = InputMode.Move;
                break;
            case InputMode.SpecialAbility:
                _hunter.CmdUseSpecial(nodeId);
                currentMode = InputMode.Move;
                break;
        }
    }

    public void SetMode(InputMode mode) => currentMode = mode;
}
