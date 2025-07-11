using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    [Header("Audio Tracks")]
    [SerializeField] private string startingMusicTrackName = "MenuTheme";
    [SerializeField] private string startingAmbienceTrackName = "ForestAmbience";

    private void Awake()
    {
        // Initialize the grid
        gridManager.InitializeGrid();

        // Start music and ambience
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(startingMusicTrackName);
            AudioManager.Instance.PlayAmbience(startingAmbienceTrackName);
        }
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
