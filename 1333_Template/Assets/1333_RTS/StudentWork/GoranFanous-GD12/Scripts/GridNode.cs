using System.Collections;
using System.Collections.Generic;
using UnityEngine;



    [System.Serializable]
public class GridNode
{
    // The name or identifier of the node (often used for debugging or labeling)
    public string Name;
    // The world-space position of this node in the game world
    public Vector3 WorldPosition;
    // Whether or not this node is walkable (i.e., can a unit move through it)
    public bool Walkable;
    // Optional additional weight or movement cost of this node (can represent terrain difficulty)
    public int Weight;
    // Cost from the start node to this node (G cost in A* pathfinding)
    public int GCost = int.MaxValue;
    // Total cost from start to end through this node (F = G + H in A* pathfinding)
    public int FCost = int.MaxValue;
    // Heuristic cost estimate from this node to the end node (H cost in A* pathfinding)
    public int HCost = int.MaxValue;
    // The direction or world-space vector to the node this node is connected to (used for backtracking path)
    public Vector3 Connection;
    // The type of terrain on this node, which can influence movement cost and walkability
    public TerrainType TerrainTypes;
}

