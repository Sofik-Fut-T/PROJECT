using UnityEngine;

public enum CellHighlight { None, Clue, Suspected, Confirmed }

public class GridCell : MonoBehaviour
{
    public Vector2Int coords;

    private Renderer _rend;

    private static readonly Color ColNormal    = new Color(0.55f, 0.55f, 0.45f);
    private static readonly Color ColClue      = new Color(1.00f, 0.85f, 0.20f);
    private static readonly Color ColSuspected = new Color(1.00f, 0.40f, 0.10f);
    private static readonly Color ColConfirmed = new Color(1.00f, 0.15f, 0.15f);

    private void Awake() => _rend = GetComponentInChildren<Renderer>();

    public void SetHighlight(CellHighlight h)
    {
        if (_rend == null) return;
        _rend.material.color = h switch
        {
            CellHighlight.Clue      => ColClue,
            CellHighlight.Suspected => ColSuspected,
            CellHighlight.Confirmed => ColConfirmed,
            _                       => ColNormal
        };
    }
}
