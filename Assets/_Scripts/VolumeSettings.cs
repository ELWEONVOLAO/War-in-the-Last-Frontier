using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Audio Mixer Principal")]
    public AudioMixer mainMixer;

    [Header("Sliders de la UI")]
    public Slider sliderMaster;
    public Slider sliderMusica;
    public Slider sliderSFX;
    public Slider sliderAmbiente;

    void Start()
    {
        // 1. Cargamos el volumen que el jugador guardó la última vez (por defecto será 1, el máximo)
        if (sliderMaster != null) sliderMaster.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        if (sliderMusica != null) sliderMusica.value = PlayerPrefs.GetFloat("MusicaVol", 1f);
        if (sliderSFX != null) sliderSFX.value = PlayerPrefs.GetFloat("SFXVol", 1f);
        if (sliderAmbiente != null) sliderAmbiente.value = PlayerPrefs.GetFloat("AmbienteVol", 1f);

        // 2. Aplicamos esos volúmenes al Mixer
        SetMasterVolume();
        SetMusicaVolume();
        SetSFXVolume();
        SetAmbienteVolume();
    }

    // Estas funciones convierten el 0 a 1 del Slider en -80 a 0 decibelios (Mathf.Log10)
    public void SetMasterVolume()
    {
        float vol = sliderMaster.value;
        mainMixer.SetFloat("MasterVol", Mathf.Log10(vol) * 20);
        PlayerPrefs.SetFloat("MasterVol", vol); // Guardamos en el PC
    }

    public void SetMusicaVolume()
    {
        float vol = sliderMusica.value;
        mainMixer.SetFloat("MusicaVol", Mathf.Log10(vol) * 20);
        PlayerPrefs.SetFloat("MusicaVol", vol);
    }

    public void SetSFXVolume()
    {
        float vol = sliderSFX.value;
        mainMixer.SetFloat("SFXVol", Mathf.Log10(vol) * 20);
        PlayerPrefs.SetFloat("SFXVol", vol);
    }

    public void SetAmbienteVolume()
    {
        float vol = sliderAmbiente.value;
        mainMixer.SetFloat("AmbienteVol", Mathf.Log10(vol) * 20);
        PlayerPrefs.SetFloat("AmbienteVol", vol);
    }
}