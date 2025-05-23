using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public class UnitSpawner : MonoBehaviour
{
    public GameObject PlayerUnit;
    public Transform Target;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Pathfinder pathfinder;

    private void Start()
    {
        SpawnUnits();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            SpawnUnits();        
        }
    }
    private void SpawnUnits()
    {
        Vector3 playerPos = GetRandomWalkablePosition();
        Vector3 targetPos = GetRandomWalkablePosition();

        while (targetPos == playerPos)
        {
            targetPos = GetRandomWalkablePosition();
        }

        Instantiate(PlayerUnit, playerPos, Quaternion.identity);
        Instantiate(Target, targetPos, Quaternion.identity);

        pathfinder.VisualizePath(playerPos, targetPos);
    }

    private Vector3 GetRandomWalkablePosition()
    {
        GridSettings settings = gridManager.GridSettings;
        Vector3 pos;

        while (true)
        {
            int x = Random.Range(0, settings.GridSizeX);
            int y = Random.Range(0, settings.GridSizeY);
            

            GridNode node = gridManager.GetNode(x, y);
            if (node != null && node.Walkable)
            {
                pos = node.WorldPosition;
                break;
            }
        }
        return pos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, Target.position);
    }
}
