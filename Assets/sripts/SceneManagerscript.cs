using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerscript : MonoBehaviour
{
        public GameObject pauseMenuUI; // Nastav v Inspectoru na panel s pauzovacím menu
    // Přepne na scénu s názvem "Game"
    public void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    // Přepne na scénu s názvem "Settings"
    public void LoadSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    // Přepne zpět do menu (scéna "Menu")
    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
    public void LoadTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Tutorial");
    }

    // Ukončí hru (funguje v buildu, v editoru zastaví přehrávání)
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
        public void PauseGame()
    {
        Time.timeScale = 0f;
        if (pauseMenuUI) pauseMenuUI.SetActive(true);
    }

    // Pokračuje ve hře a skryje menu
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
    }

    /*// Uloží hlasitost hudby a zvuků do PlayerPrefs
    public void SaveSettings(float musicVolume, float sfxVolume)
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    // Načte hlasitost hudby a zvuků z PlayerPrefs
    public void LoadSettingsValues(out float musicVolume, out float sfxVolume)
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }*/
}
    