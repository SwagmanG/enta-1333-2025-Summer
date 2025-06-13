using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public float speed = 2f;
    private List<Vector3> path;
    private int currentIndex = 0;

    public ArmyType armyType;
    private ArmyController armyController;

    private AstarPathfinding pathfinder; //  A* ref per unit

    private void Start()
    {
        armyController = FindFirstObjectByType<ArmyController>();
        if (armyController != null)
            armyController.RegisterUnit(this, armyType);
    }

    public void SetPathfinder(AstarPathfinding pf)
    {
        pathfinder = pf != null ? pf : FindFirstObjectByType<AstarPathfinding>();
    }

    public void RequestPath(Vector3 targetPosition)
    {
        if (pathfinder != null)
        {
            pathfinder.VisualizePath(transform.position, targetPosition, this);
        }
        else
        {
            Debug.LogWarning("Pathfinder not assigned to unit!");
        }
    }

    public void FollowPath(List<GridNode> gridPath)
    {
        path = new List<Vector3>();
        foreach (var node in gridPath)
            path.Add(node.WorldPosition + Vector3.up * 0.5f);

        currentIndex = 0;
        StopAllCoroutines();
        StartCoroutine(FollowPathCoroutine());
    }

    private IEnumerator FollowPathCoroutine()
    {
        while (currentIndex < path.Count)
        {
            while (Vector3.Distance(transform.position, path[currentIndex]) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, path[currentIndex], speed * Time.deltaTime);
                yield return null;
            }
            currentIndex++;
        }
    }

    private void OnDestroy()
    {
        if (armyController != null)
            armyController.RemoveUnit(this);
    }

    public void StopMovement()
    {
        StopAllCoroutines();
        if (path != null)
            path.Clear();
    }
}