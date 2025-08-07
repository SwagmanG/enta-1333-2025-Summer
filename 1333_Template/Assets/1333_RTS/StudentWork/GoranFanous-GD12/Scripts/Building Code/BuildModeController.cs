using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the build mode functionality for the game
/// </summary>
public class BuildModeController : MonoBehaviour
{
    // Singleton instance for easy access from other scripts
    public static BuildModeController BMCInstance { get; private set; }

    // Reference to the UI manager for showing/hiding build interface
    public GameUiManager UIManager;

    // Tracks whether the player is currently in build mode
    public bool IsInBuildMode { get; private set; } = false;

    /// <summary>
    /// Set up the singleton pattern when the object is created
    /// </summary>
    private void Awake()
    {
        // Make sure only one BuildModeController exists at a time
        if (BMCInstance != null && BMCInstance != this)
        {
            Destroy(this);
        }
        else
        {
            BMCInstance = this;
        }
    }

    /// <summary>
    /// Check for build mode toggle input every frame
    /// </summary>
    private void Update()
    {
        // Listen for the B key to toggle build mode on/off
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Flip the build mode state
            IsInBuildMode = !IsInBuildMode;
            Debug.Log("Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled"));

            // Play build mode toggle sound
            AudioManager.AMInstance?.PlaySFX("Enter/Exit Buildmode");
            Debug.LogWarning("buildmode sound");

            // Show or hide the build panel based on the build mode state
            if (IsInBuildMode)
                UIManager.ShowBuildUI();
            else
                UIManager.HideBuildUI();
        }
    }
}