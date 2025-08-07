using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles button interactions and their associated audio feedback
/// </summary>
public class ButtonManager : MonoBehaviour
{
    /// <summary>
    /// Called when any button is clicked - plays a sound effect
    /// </summary>
    public void OnButtonClicked()
    {
        // Play the button click sound effect
        AudioManager.AMInstance?.PlaySFX("Enter/Exit Buildmode");
    }
}