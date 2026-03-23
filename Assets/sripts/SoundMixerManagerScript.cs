using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    public static SoundMixerManager Instance;

    [SerializeField] private AudioMixer audioMixer;
    public AudioMixer AudioMixer => audioMixer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetMasterVolume(float level)
    {
        level = Mathf.Clamp(level, 0.0001f, 1f);
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(level) * 20f);
    }

    public void SetSoundFXVolume(float level)
    {
        level = Mathf.Clamp(level, 0.0001f, 1f);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(level) * 20f);
    }

    public void SetMusicVolume(float level)
    {
        level = Mathf.Clamp(level, 0.0001f, 1f);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(level) * 20f);
    }
}