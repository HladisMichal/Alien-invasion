using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerscript : MonoBehaviour
{
    public static string PendingSceneAfterMenu = "";
    public GameObject pauseMenuUI; // Nastav v Inspectoru na panel s pauzovacím menu
    private bool isPaused = false;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Menu" && !string.IsNullOrEmpty(PendingSceneAfterMenu))
        {
            string targetScene = PendingSceneAfterMenu;
            PendingSceneAfterMenu = "";
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetScene);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale == 0f && !isPaused)
                return;
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }
    // Přepne na scénu s názvem "Game"
   public void LoadGame()
{
    // 1. Najdeme starou hudbu
    GameObject menuMusic = GameObject.Find("MusicManagerMenu");
    if (menuMusic != null) 
    {
        // 2. Vynulujeme statickou referenci, aby si nový manager nemyslel, že je plno
        MusicManager.Instance = null; 
        // 3. Smažeme starý objekt
        Destroy(menuMusic);
    }

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
    GameObject gameMusic = GameObject.Find("MusicManagerGame");
    if (gameMusic != null) 
    {
        MusicManager.Instance = null; // Důležité: uvolnit referenci!
        Destroy(gameMusic);
    }

    Time.timeScale = 1f;
    SceneManager.LoadScene("Menu");
}
    public void LoadTutorial()
{
    // Tutorial bere herní hudbu, takže smažeme menu hudbu
    GameObject menuMusic = GameObject.Find("MusicManagerMenu");
    if (menuMusic != null) Destroy(menuMusic);

    Time.timeScale = 1f;
    SceneManager.LoadScene("Tutorial");
}

   public void LoadStatistic()
{
    // Pokud jdeme do statistik ze hry, smažeme herní hudbu
    GameObject gameMusic = GameObject.Find("MusicManagerGame");
    GameObject menuMusic = GameObject.Find("MusicManagerMenu");

    // Ze hry do statistik: pokud menu hudba neběží, jednorázově projdeme přes Menu.
    if (gameMusic != null && menuMusic == null)
    {
        MusicManager.Instance = null;
        Destroy(gameMusic);
        Time.timeScale = 1f;
        PendingSceneAfterMenu = "Statistic";
        SceneManager.LoadScene("Menu");
        return;
    }

    if (gameMusic != null)
    {
        MusicManager.Instance = null;
        Destroy(gameMusic);
    }

    Time.timeScale = 1f;
    SceneManager.LoadScene("Statistic");
}
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
        if (Time.timeScale == 0f && !isPaused)
            return;
        Time.timeScale = 0f;
        if (pauseMenuUI) pauseMenuUI.SetActive(true);
            isPaused = true;
    }
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        isPaused = false;
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
    