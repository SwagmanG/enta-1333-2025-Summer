using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingSettings", menuName = "Game/BuildingSettings")]
public class BuildingSettings : ScriptableObject
{
    //Private Fields for settings
    [SerializeField] private string buildingName = "Default";
    [SerializeField] private int buildingSizeX = 1;
    [SerializeField] private int buildingSizeY = 1;
    [SerializeField] private float buildScale = 1;
    [SerializeField] private int maxHealth = 1;

    //Exposing private variables.
    public string BuildingName => buildingName;
    public int BuildingSizeX => buildingSizeX;
    public int BuildingSizeY => buildingSizeY;
    public float BuildScale => buildScale;
    public int MaxHealth => maxHealth;

    //Resource Costs and food tax.
    public int PopulationCost = 1;
    public int FoodTax = 1;
    public int GoldCost = 1;

    //Modifier alterations.
    public float GoldMod = 1;
    public float PopulationMod = 1;
    public float FoodMod = 1;
}
