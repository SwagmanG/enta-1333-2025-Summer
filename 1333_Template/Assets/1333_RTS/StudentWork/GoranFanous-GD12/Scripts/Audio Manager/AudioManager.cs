using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Audio source components - each handles a different type of sound
    [Header("Audio Sources")]
    public AudioSource MusicSource;      // For menu or main BGM
    public AudioSource AmbienceSource;   // For ambient/game BGM
    public AudioSource SfxSource;        // For sound effects

    // Lists to hold our sound settings - makes it easy to organize in the inspector
    [Header("Music Settings")]
    public List<SoundSettings> MusicTracks;

    [Header("Ambience Settings")]
    public List<SoundSettings> AmbienceTracks;

    [Header("SFX Settings")]
    public List<SoundSettings> SfxClips;

    // Simple toggles to enable/disable different audio categories
    [Header("Audio Toggles")]
    public bool SfxEnabled = true;
    public bool MusicEnabled = true;
    public bool AmbienceEnabled = true;

    // Singleton instance - ensures we only have one AudioManager across scenes
    public static AudioManager AMInstance;

    void Awake()
    {
        // Standard singleton pattern - keep this instance alive and destroy any duplicates
        if (AMInstance == null)
        {
            AMInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === MUSIC METHODS ===
    // Play background music by name - automatically loops
    public void PlayMusic(string name)
    {
        if (!MusicEnabled) return;

        AudioClip clip = GetClipByName(MusicTracks, name);
        if (clip != null)
        {
            MusicSource.clip = clip;
            MusicSource.loop = true;
            MusicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music track not found: " + name);
        }
    }

    // Stop the currently playing music
    public void StopMusic()
    {
        MusicSource.Stop();
    }

    // === AMBIENCE METHODS ===
    // Play ambient sounds - things like wind, rain, or environmental audio
    public void PlayAmbience(string name)
    {
        if (!AmbienceEnabled) return;

        AudioClip clip = GetClipByName(AmbienceTracks, name);
        if (clip != null)
        {
            AmbienceSource.clip = clip;
            AmbienceSource.loop = true;
            AmbienceSource.Play();
        }
        else
        {
            Debug.LogWarning("Ambience track not found: " + name);
        }
    }

    // Stop ambient sounds
    public void StopAmbience()
    {
        AmbienceSource.Stop();
    }

    // === SOUND EFFECTS METHODS ===
    // Play a one-shot sound effect - doesn't interrupt other SFX
    public void PlaySFX(string name)
    {
        if (!SfxEnabled) return;

        AudioClip clip = GetClipByName(SfxClips, name);
        if (clip != null)
        {
            SfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SFX clip not found: " + name);
        }
    }

    // Play a sound effect at a specific world position - great for 3D audio
    public void PlaySFXAtPosition(string name, Vector3 position)
    {
        if (!SfxEnabled) return;

        AudioClip clip = GetClipByName(SfxClips, name);
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
        else
        {
            Debug.LogWarning("SFX clip not found at position: " + name);
        }
    }

    // === UTILITY METHODS ===
    // Helper method to find audio clips by name in our lists
    private AudioClip GetClipByName(List<SoundSettings> list, string name)
    {
        foreach (var item in list)
        {
            if (item.Name == name)
                return item.AudioClip;
        }
        return null;
    }

    // === TOGGLE METHODS ===
    // These methods let you enable/disable different audio categories at runtime
    public void ToggleSFX(bool enabled)
    {
        SfxEnabled = enabled;
        if (!enabled) SfxSource.Stop();
    }

    public void ToggleMusic(bool enabled)
    {
        MusicEnabled = enabled;
        if (!enabled) MusicSource.Stop();
    }

    public void ToggleAmbience(bool enabled)
    {
        AmbienceEnabled = enabled;
        if (!enabled) AmbienceSource.Stop();
    }
}