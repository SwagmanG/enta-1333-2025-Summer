using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="BuildingTypes", menuName = "ScriptableObject/BuildingTypes")]

public class BuildingTypesSo : ScriptableObject
{
    public List<BuildingData> Buildings = new();
}

[System.Serializable]
public class BuildingData
{
    public string BuildingName;
    public Sprite Icon;
    public int Cost;
    public int Width;
    public int Length;
}
