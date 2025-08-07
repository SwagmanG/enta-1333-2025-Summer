using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSlider : MonoBehaviour
{
    public enum VolumeType { Master = 0, Music = 1, Ambience = 3, SFX = 2}
    [SerializeField] private VolumeType volumeType;
    [SerializeField] private AudioMixer audioMixer;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void Start()
    {
        float value;
        string param = GetParamName();
        if (audioMixer.GetFloat(param, out value))
        {
            slider.value = Mathf.Pow(10f, value / 20f); // convert dB to 0–1 range
        }
    }

    private void SetVolume(float value)
    {
        string param = GetParamName();
        float decibel = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f; // convert 0–1 to dB
        audioMixer.SetFloat(param, decibel);
    }

    private string GetParamName()
    {
        return volumeType.ToString() + "Volume"; // e.g., "MusicVolume"
    }
}
