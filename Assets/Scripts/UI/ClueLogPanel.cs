using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClueLogPanel : MonoBehaviour
{
    public static ClueLogPanel Instance { get; private set; }

    [Header("Layout")]
    public Transform   container;     // Vertical Layout Group parent
    public GameObject  entryPrefab;   // Prefab with TextMeshProUGUI

    private readonly List<(int round, GameObject go)> _entries = new();

    private void Awake() => Instance = this;

    public void AddEntry(ClueData clue)
    {
        var go   = Instantiate(entryPrefab, container);
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = clue.Describe();
        if (clue.isFake) text.color = new Color(1f, 0.5f, 0.5f);  // red tint for fakes
        _entries.Add((clue.round, go));
    }

    public void RemoveEntry(int round)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].round == round)
            {
                Destroy(_entries[i].go);
                _entries.RemoveAt(i);
                return;
            }
        }
    }
}
