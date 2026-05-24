using System;

public enum ClueType { Zone, Fake }

[Serializable]
public struct ClueData
{
    public ClueType type;
    public int      round;
    public string   zoneName;
    public bool     isFake;

    public string Describe()
    {
        if (isFake) return $"[Раунд {round}] Підозрілий слід...";
        string zone = string.IsNullOrEmpty(zoneName) ? "нейтральна" : zoneName;
        return $"[Раунд {round}] Звір в зоні: {zone}";
    }
}
