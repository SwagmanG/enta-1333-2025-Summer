using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAIController : MonoBehaviour
{
    public Transform currentTarget;
    public AstarPathfinding pathfinder; // Optional override to use a shared pathfinder
    private UnitController unitController;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
        if (pathfinder == null)
            pathfinder = FindFirstObjectByType<AstarPathfinding>(); // fallback if not assigned
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;

        if (pathfinder != null && currentTarget != null)
        {
            pathfinder.VisualizePath(transform.position, currentTarget.position, unitController);
        }
    }

    public void MoveToPosition(Vector3 worldPos)
    {
        if (pathfinder != null)
        {
            pathfinder.VisualizePath(transform.position, worldPos, unitController);
        }
    }
}

