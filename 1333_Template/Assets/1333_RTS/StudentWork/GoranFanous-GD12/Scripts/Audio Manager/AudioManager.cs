using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource;      // For menu or main BGM
    public AudioSource ambienceSource;   // For ambient/game BGM
    public AudioSource sfxSource;

    [Header("Music Settings")]
    public List<SoundSettings> musicTracks;

    [Header("Ambience Settings")]
    public List<SoundSettings> ambienceTracks;

    [Header("SFX Settings")]
    public List<SoundSettings> sfxClips;

    [Header("Audio Toggles")]
    public bool sfxEnabled = true;
    public bool musicEnabled = true;
    public bool ambienceEnabled = true;

    public static AudioManager AMInstance;

    void Awake()
    {
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

    // === MUSIC ===
    public void PlayMusic(string name)
    {
        if (!musicEnabled) return;

        AudioClip clip = GetClipByName(musicTracks, name);
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music track not found: " + name);
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // === AMBIENCE ===
    public void PlayAmbience(string name)
    {
        if (!ambienceEnabled) return;

        AudioClip clip = GetClipByName(ambienceTracks, name);
        if (clip != null)
        {
            ambienceSource.clip = clip;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }
        else
        {
            Debug.LogWarning("Ambience track not found: " + name);
        }
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
    }

    // === SFX ===
    public void PlaySFX(string name)
    {
        if (!sfxEnabled) return;

        AudioClip clip = GetClipByName(sfxClips, name);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SFX clip not found: " + name);
        }
    }

    public void PlaySFXAtPosition(string name, Vector3 position)
    {
        if (!sfxEnabled) return;

        AudioClip clip = GetClipByName(sfxClips, name);
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
        else
        {
            Debug.LogWarning("SFX clip not found at position: " + name);
        }
    }

    // === CLIP LOOKUP ===
    private AudioClip GetClipByName(List<SoundSettings> list, string name)
    {
        foreach (var item in list)
        {
            if (item.Name == name)
                return item.AudioClip;
        }
        return null;
    }

    // === TOGGLES ===
    public void ToggleSFX(bool enabled)
    {
        sfxEnabled = enabled;
        if (!enabled) sfxSource.Stop();
    }

    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        if (!enabled) musicSource.Stop();
    }

    public void ToggleAmbience(bool enabled)
    {
        ambienceEnabled = enabled;
        if (!enabled) ambienceSource.Stop();
    }
}
