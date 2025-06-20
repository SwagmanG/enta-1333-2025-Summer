using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the AI-driven movement behavior for a unit.
/// Responsible for issuing pathfinding requests to the UnitController.
/// </summary>
public class UnitAIController : MonoBehaviour
{
    // Reference to the unit's controller component responsible for pathing and movement
    private UnitController unitMovementController;

    private void Awake()
    {
        // Get the UnitController component attached to the same GameObject
        unitMovementController = GetComponent<UnitController>();
    }

    /// <summary>
    /// Requests movement toward a specific target Transform in the world.
    /// </summary>
    /// <param name="targetTransform">The transform the unit should move toward.</param>
    public void MoveToTarget(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            unitMovementController.RequestPath(targetTransform.position);
        }
    }

    /// <summary>
    /// Requests movement toward a specific world position.
    /// </summary>
    /// <param name="destinationWorldPosition">The position the unit should move toward.</param>
    public void MoveToPosition(Vector3 destinationWorldPosition)
    {
        unitMovementController.RequestPath(destinationWorldPosition);
    }
}
