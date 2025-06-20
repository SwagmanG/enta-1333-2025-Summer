using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public ArmyType armyType;

    private Coroutine movementCoroutine;
    private int currentPathIndex;
    private List<GridNode> currentPath;
    private float movementSpeed = 3f;

    private GridManager gridManager;
    private GridNode finalOccupiedNode;
    private Vector3 targetWorldPosition;
    private bool isAttemptingRepath = false;

    private void Awake()
    {
        // Get reference to GridManager in the scene
        gridManager = FindFirstObjectByType<GridManager>();
    }

    /// <summary>
    /// Requests a path to the specified world position using the A* system.
    /// </summary>
    public void RequestPath(Vector3 worldDestinationPosition)
    {
        targetWorldPosition = worldDestinationPosition;
        AstarPathfinding.Instance?.RequestPathfinding(transform.position, worldDestinationPosition, this);
    }

    /// <summary>
    /// Begins moving along a given path of GridNodes.
    /// </summary>
    public void FollowPath(List<GridNode> pathToFollow)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        currentPath = pathToFollow;
        currentPathIndex = 0;

        // Unmark the last occupied node before starting a new movement
        if (finalOccupiedNode != null)
        {
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
            finalOccupiedNode = null;
        }

        isAttemptingRepath = false;
        movementCoroutine = StartCoroutine(FollowPathCoroutine());
    }

    /// <summary>
    /// Coroutine to move the unit along the path step by step.
    /// Waits if the target node is occupied and attempts to repath if blocked too long.
    /// </summary>
    private IEnumerator FollowPathCoroutine()
    {
        while (currentPathIndex < currentPath.Count)
        {
            GridNode nextNode = currentPath[currentPathIndex];
            Vector3 worldTargetPosition = nextNode.WorldPosition + Vector3.up * 0.5f;

            float waitDuration = 0f;
            while (gridManager.IsOccupied(nextNode) && !IsOnSameNode(nextNode))
            {
                waitDuration += 0.1f;

                // If stuck too long on final node, try repathing to nearby neighbors
                if (waitDuration > 2f && currentPathIndex == currentPath.Count - 1 && !isAttemptingRepath)
                {
                    isAttemptingRepath = true;
                    yield return StartCoroutine(TryRepathAroundGoalNode());
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Move smoothly toward the next node's position
            while (Vector3.Distance(transform.position, worldTargetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, worldTargetPosition, movementSpeed * Time.deltaTime);
                yield return null;
            }

            currentPathIndex++;
            yield return null;
        }

        // Once movement is finished, mark the final node as occupied
        GridNode currentFinalNode = GetNodeUnderUnit();
        if (currentFinalNode != null)
        {
            gridManager.MarkOccupied(currentFinalNode, this);
            finalOccupiedNode = currentFinalNode;
        }
    }

    /// <summary>
    /// Attempts to find an alternate walkable, unoccupied neighbor node around the goal.
    /// </summary>
    private IEnumerator TryRepathAroundGoalNode()
    {
        Vector2Int goalGridCoordinates = gridManager.GetGridPosFromWorld(targetWorldPosition);
        GridNode goalNode = gridManager.GetNode(goalGridCoordinates.x, goalGridCoordinates.y);

        if (goalNode == null)
            yield break;

        // Get walkable neighbors around the goal node
        List<GridNode> neighboringNodes = gridManager.GetWalkableNeighbors(goalNode, this);

        // Sort by proximity to the current unit's position
        neighboringNodes.Sort((nodeA, nodeB) =>
            Vector3.Distance(transform.position, nodeA.WorldPosition)
            .CompareTo(Vector3.Distance(transform.position, nodeB.WorldPosition))
        );

        // Try to find an unoccupied alternative
        foreach (GridNode alternativeTargetNode in neighboringNodes)
        {
            if (!gridManager.IsOccupied(alternativeTargetNode))
            {
                RequestPath(alternativeTargetNode.WorldPosition);
                yield break;
            }
        }

        // If no alternative found, retry original goal after delay
        yield return new WaitForSeconds(0.5f);
        RequestPath(targetWorldPosition);
    }

    /// <summary>
    /// Gets the grid node directly under the unit's current position.
    /// </summary>
    private GridNode GetNodeUnderUnit()
    {
        Vector2Int unitGridPosition = gridManager.GetGridPosFromWorld(transform.position);
        return gridManager.GetNode(unitGridPosition.x, unitGridPosition.y);
    }

    /// <summary>
    /// Checks if the unit is currently on the specified grid node.
    /// </summary>
    private bool IsOnSameNode(GridNode nodeToCheck)
    {
        return nodeToCheck == GetNodeUnderUnit();
    }

    /// <summary>
    /// Stops movement and unmarks the currently occupied node, if any.
    /// </summary>
    public void StopMovement()
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        if (finalOccupiedNode != null)
        {
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
            finalOccupiedNode = null;
        }
    }
}
