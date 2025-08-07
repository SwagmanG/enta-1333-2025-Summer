using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager that handles initialization and setup
/// </summary>
public class GameManager : MonoBehaviour
{
    // Reference to the grid system for the game world
    [SerializeField] private GridManager gridManager;

    [Header("Audio Tracks")]
    // Name of the music track to play when the game starts
    [SerializeField] private string startingMusicTrackName = "MenuTheme";
    // Name of the ambient sound to play in the background
    [SerializeField] private string startingAmbienceTrackName = "ForestAmbience";

    /// <summary>
    /// Initialize core systems as early as possible
    /// </summary>
    private void Awake()
    {
        // Initialize the grid
        gridManager.InitializeGrid();
    }

    /// <summary>
    /// Start audio and other systems that depend on everything being loaded
    /// </summary>
    private void Start()
    {
        // Delay audio playback until Start to ensure AudioManager is fully initialized
        if (AudioManager.AMInstance != null)
        {
            // Start playing the background music
            AudioManager.AMInstance.PlayMusic(startingMusicTrackName);
            // Start playing ambient environmental sounds
            AudioManager.AMInstance.PlayAmbience(startingAmbienceTrackName);
        }
        else
        {
            // Log a warning if the AudioManager isn't available
            Debug.LogWarning("AudioManager.Instance is null in GameManager Start.");
        }
    }
}