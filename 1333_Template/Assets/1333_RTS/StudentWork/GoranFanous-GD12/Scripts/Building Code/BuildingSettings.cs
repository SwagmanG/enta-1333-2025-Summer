using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingSettings", menuName = "Game/BuildingSettings")]
public class BuildingSettings : ScriptableObject
{
    // The display name of the building (used in UI or debugging)
    [SerializeField] private string buildingName = "Default";

    // The size of the building along the grid's X axis (width in grid nodes)
    [SerializeField] private int buildingSizeX = 1;

    // The size of the building along the grid's Y axis (height in grid nodes)
    [SerializeField] private int buildingSizeY = 1;

    // The scale factor for the building's visual representation
    [SerializeField] private float buildScale = 1;

    // Maximum health points of the building (hit points)
    [SerializeField] private int maxHealth = 1;

    // Current health points of the building (can decrease during gameplay)
    [SerializeField] private int currentHealth = 1;

    // Public getter for the building's name
    public string BuildingName => buildingName;

    // Public getter for the building's size in X (width)
    public int BuildingSizeX => buildingSizeX;

    // Public getter for the building's size in Y (height)
    public int BuildingSizeY => buildingSizeY;

    // Public getter for the building's scale factor
    public float BuildScale => buildScale;

    // Public getter for the maximum health value
    public int MaxHealth => maxHealth;

    // Public getter for the current health value
    public int CurrentHealth => currentHealth;
}
