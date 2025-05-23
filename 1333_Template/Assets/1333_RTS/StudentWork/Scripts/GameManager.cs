using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    
    private void Awake()
    {
        gridManager.InitializeGrid();
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Awake();
        }
    }
}
