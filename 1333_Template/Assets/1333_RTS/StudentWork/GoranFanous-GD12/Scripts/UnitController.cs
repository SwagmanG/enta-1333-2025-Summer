using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public float speed = 2f;                 // Movement speed of the unit
    private List<Vector3> path;              // World positions representing the path to follow
    private int currentIndex = 0;            // Current index in the path the unit is moving towards

    public ArmyType armyType;                // The army faction this unit belongs to
    private ArmyController armyController;  // Reference to the army controller managing units

    private void Start()
    {
        // Find the first active ArmyController instance in the scene
        armyController = FindFirstObjectByType<ArmyController>();

        // If found, register this unit with its army type
        if (armyController != null)
        {
            armyController.RegisterUnit(this, armyType);
        }
    }

    // Starts following a given path of GridNodes
    public void FollowPath(List<GridNode> gridPath)
    {
        path = new List<Vector3>();

        // Convert each grid node's world position to a path position with a vertical offset
        foreach (var node in gridPath)
            path.Add(node.WorldPosition + Vector3.up * 0.5f);

        currentIndex = 0;

        // Stop any ongoing movement coroutine before starting a new one
        StopAllCoroutines();
        StartCoroutine(FollowPathCoroutine());
    }

    // Coroutine to move the unit along the path smoothly
    private IEnumerator FollowPathCoroutine()
    {
        // Continue until the end of the path is reached
        while (currentIndex < path.Count)
        {
            // Move towards the current path position until close enough
            while (Vector3.Distance(transform.position, path[currentIndex]) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, path[currentIndex], speed * Time.deltaTime);
                yield return null;
            }
            currentIndex++; // Move to the next waypoint in the path
        }
    }

    // Called when the unit is destroyed
    private void OnDestroy()
    {
        // Remove the unit from the army controller if it exists
        if (armyController != null)
        {
            armyController.RemoveUnit(this);
        }
    }

    // Stops the unit's movement and clears the current path
    public void StopMovement()
    {
        StopAllCoroutines(); // Stops any ongoing movement coroutine

        if (path != null)
        {
            path.Clear(); // Clear the current path to stop further movement
        }
    }
}