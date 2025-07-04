using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridSettings", menuName = "Game/GridSettings")]
public class GridSettings : ScriptableObject
{
    // Number of nodes along the X axis (width of the grid)
    [SerializeField] private int gridSizeX = 10;

    // Number of nodes along the Y axis (height or depth of the grid)
    [SerializeField] private int gridSizeY = 10;

    // Size (width and length) of each individual grid node in world units
    [SerializeField] private float nodeSize = 1f;

    // Whether to use the XZ plane (true) or XY plane (false) for the grid layout
    [SerializeField] private bool useXZPlane = true;

    /// <summary>
    /// Public getter for the grid width in nodes.
    /// </summary>
    public int GridSizeX => gridSizeX;

    /// <summary>
    /// Public getter for the grid height (or depth) in nodes.
    /// </summary>
    public int GridSizeY => gridSizeY;

    /// <summary>
    /// Public getter for the size of each grid node.
    /// </summary>
    public float NodeSize => nodeSize;

    /// <summary>
    /// Public getter indicating whether the grid uses the XZ plane or XY plane.
    /// </summary>
    public bool UseXZPlane => useXZPlane;
}
