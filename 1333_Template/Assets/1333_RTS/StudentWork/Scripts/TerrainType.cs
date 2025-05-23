using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainType", menuName = "Game/TerrainType")]
public class TerrainType : ScriptableObject
{
    [SerializeField] private string terrainName = "Default";
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private bool walkable = true;
    [SerializeField] private int movementCost = 1;

    public string TerrainName => terrainName;
    public Color GizmoColor => gizmoColor;
    public bool IsWalkable => walkable;
    public int MovementCost => movementCost;


}
