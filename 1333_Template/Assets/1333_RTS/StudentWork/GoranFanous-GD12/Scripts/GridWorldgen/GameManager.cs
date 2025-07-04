using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        // Initialize the grid once when the game starts
        gridManager.InitializeGrid();
    }

    private void Update()
    {
        // Press R to reinitialize the grid at runtime
        if (Input.GetKeyDown(KeyCode.R))
        {
            gridManager.InitializeGrid();
        }
    }
}
