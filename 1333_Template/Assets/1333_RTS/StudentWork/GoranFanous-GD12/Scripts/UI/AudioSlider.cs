using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSlider : MonoBehaviour
{
    // Define the different types of audio we can control
    // Note: The numbers correspond to specific mixer group indices
    public enum VolumeType { Master = 0, Music = 1, Ambience = 3, SFX = 2 }

    // Configuration for this specific slider
    [SerializeField] private VolumeType volumeType;  // Which audio category this slider controls
    [SerializeField] private AudioMixer audioMixer; // Reference to the audio mixer

    // Component reference - gets the slider component on this GameObject
    private Slider slider;

    private void Awake()
    {
        // Get the slider component and set up the callback
        slider = GetComponent<Slider>();
        // When the slider value changes, call our SetVolume method
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void Start()
    {
        // === INITIALIZE SLIDER WITH CURRENT MIXER VALUE ===
        // Load the current volume setting from the audio mixer and set the slider to match

        float value;
        string param = GetParamName();

        // Try to get the current volume from the mixer
        if (audioMixer.GetFloat(param, out value))
        {
            // Convert from decibels back to 0-1 range for the slider
            // This ensures the slider shows the correct position when the scene loads
            slider.value = Mathf.Pow(10f, value / 20f); // convert dB to 0–1 range
        }
    }

    // === VOLUME CONTROL METHOD ===
    // This gets called whenever the player moves the slider

    // Convert slider value to decibels and apply to the audio mixer
    private void SetVolume(float value)
    {
        string param = GetParamName();

        // Convert the 0-1 slider value to decibels (dB)
        // Audio mixers work in decibels, but sliders work in 0-1 range
        // We clamp to 0.0001f minimum to avoid log10(0) which would be -infinity
        float decibel = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f; // convert 0–1 to dB

        // Apply the new volume to the audio mixer
        audioMixer.SetFloat(param, decibel);
    }

    // === UTILITY METHOD ===
    // Generate the parameter name that matches our audio mixer setup

    // Create the parameter name based on the volume type
    // This should match the exposed parameters in your Audio Mixer
    private string GetParamName()
    {
        return volumeType.ToString() + "Volume"; // e.g., "MusicVolume"
    }
}