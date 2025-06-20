using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingSettings", menuName = "Game/BuildingSettings")]
public class BuildingSettings : ScriptableObject
{

    [SerializeField] private string buildingName = "Default";
    [SerializeField] private int buildSizeX = 1;
    [SerializeField] private int buildSizeY = 1;
   
    [SerializeField] private float buildScale = 1;

    [SerializeField] private bool useXZPlane = true;

    public string BuildingName => buildingName;
    public int BuildSizeX => buildSizeX;
    public int BuildSizeY => buildSizeY;
    public float BuildScale => buildScale;
    public bool UseXZPlane => useXZPlane;
}
