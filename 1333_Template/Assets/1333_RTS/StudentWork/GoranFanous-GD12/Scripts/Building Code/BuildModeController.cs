using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController BMCInstance { get; private set; }

    public GameUiManager uiManager;

    public bool IsInBuildMode { get; private set; } = false;

    private void Awake()
    {
        if (BMCInstance != null && BMCInstance != this)
        {
            Destroy(this);
        }
        else
        {
            BMCInstance = this;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            IsInBuildMode = !IsInBuildMode;
            Debug.Log("Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled"));

            // Play build mode toggle sound
            AudioManager.Instance?.PlaySFX("Enter/Exit Buildmode");
            Debug.LogWarning("buildmode sound");

            // Show or hide the build panel based on the build mode state
            if (IsInBuildMode)
                uiManager.ShowBuildUI();
            else
                uiManager.HideBuildUI();
        }
    }
}
