using System;

public enum ClueType { SameRow, SameColumn, Zone, Fake }

[Serializable]
public struct ClueData
{
    public ClueType type;
    public int      round;
    public float    lineValue;     // world Z (SameRow) or world X (SameColumn)
    public int      centerNodeId;  // Zone: center node
    public float    zoneRadius;    // Zone: radius
    public bool     isFake;

    public string Describe() => type switch
    {
        ClueType.SameRow    => $"[Раунд {round}] Звір знаходиться в підсвіченому ряду",
        ClueType.SameColumn => $"[Раунд {round}] Звір знаходиться в підсвіченій колонці",
        ClueType.Zone       => $"[Раунд {round}] Звір поблизу підсвіченої зони",
        ClueType.Fake       => $"[Раунд {round}] Підозрілий слід...",
        _                   => "Невідома підказка"
    };
}
