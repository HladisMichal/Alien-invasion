using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSliderHelper : MonoBehaviour
{
    public enum VolumeType { Master, Music, SFX }
    public VolumeType type;
    [SerializeField] private AudioMixer mixer; 

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        // Kontrola: Pokud mixer chybí v inspektoru, zkusíme ho najít u SoundMixerManageru
        if (mixer == null && SoundMixerManager.Instance != null)
        {
            // Tuto proměnnou si musíme v SoundMixerManageru udělat veřejnou (viz níže)
            // mixer = SoundMixerManager.Instance.audioMixer; 
        }

        if (mixer == null)
        {
            Debug.LogWarning($"Na slideru {gameObject.name} chybí přiřazený AudioMixer!");
            return; // Ukončí metodu dříve, než nastane Error
        }

        string parameterName = type switch
        {
            VolumeType.Master => "MasterVolume",
            VolumeType.Music => "MusicVolume",
            VolumeType.SFX => "SFXVolume",
            _ => ""
        };

        if (mixer.GetFloat(parameterName, out float dbValue))
        {
            slider.value = Mathf.Pow(10f, dbValue / 20f);
        }
    }

    public void UpdateVolume(float value)
    {
        if (SoundMixerManager.Instance != null)
        {
            switch (type)
            {
                case VolumeType.Master: SoundMixerManager.Instance.SetMasterVolume(value); break;
                case VolumeType.Music: SoundMixerManager.Instance.SetMusicVolume(value); break;
                case VolumeType.SFX: SoundMixerManager.Instance.SetSoundFXVolume(value); break;
            }
        }
    }
}