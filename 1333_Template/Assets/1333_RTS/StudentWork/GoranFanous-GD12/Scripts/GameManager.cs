using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    
    
    private void Awake()
    {

        //Run the initialize grid function
        gridManager.InitializeGrid();
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //Run the initialize grid function
            Awake();
        }
    }
}
