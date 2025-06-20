using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    public bool IsInBuildMode { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            IsInBuildMode = !IsInBuildMode;
            Debug.Log("Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled"));
        }
    }
}
