using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class minibossScript : MonoBehaviour
{
    public int druh;
    public GameObject miniboss;
    public UFOFollowPlayer[] hranice; // UFO pro bariéry arény
    public GameObject endUI; // UI, které se zobrazí po poražení minibosse
    
    private bool bossFightActive = false;
    private bool fightAlreadyStarted = false; // Aby se fight spustil jen jednou

    void Update()
    {
        
    }

    private void AktivujBariery()
    {
        foreach (UFOFollowPlayer ufo in hranice)
        {
            if (ufo != null)
            {
                ufo.gameObject.SetActive(true);
                ufo.ActivateBarrier();
            }
        }
        
        // Vypni normální UFO spawning z mapy
        if (MapGeneration.instance != null)
        {
            MapGeneration.instance.StopSpawningUFO();
        }
    }

    public void DeaktivujBariery()
    {
        foreach (UFOFollowPlayer ufo in hranice)
        {
            if (ufo != null)
            {
                // Vypni střelbu
                ufo.SetContinuousFire(false);
                // Spustí deaktivaci/odlet animaci
                ufo.DeactivateBarrier();
                // Deaktivuj UFO až po animaci (animace trvá ~2 sekundy)
                ufo.Invoke("DisableGameObject", 10f);
            }
            if (endUI != null)
            {
                endUI.SetActive(true);
                Time.timeScale = 0f; 
                
            }
        }
        
        // Znovu zapni normální UFO spawning
        if (MapGeneration.instance != null)
        {
            MapGeneration.instance.StartSpawningUFO();
        }
        
        // Unlock kamery
        PlayerScript.cameraLocked = false;
        bossFightActive = false;
    }
    
    // Spustí boss fight - zavolá se když hráč vejde do triggeru
    public void ActivateBossFight()
    {
        // Pokud se fight už jednou spustil, nepovoluj znovu
        if (fightAlreadyStarted)
            return;
        
        fightAlreadyStarted = true;
        bossFightActive = true;

        Slider bossBar = FindBossBarSlider();

        if (miniboss != null)
        {
            EnemyScript minibossEnemy = miniboss.GetComponentInChildren<EnemyScript>(true);
            if (minibossEnemy != null)
            {
                minibossEnemy.bossHealthBar = bossBar;
            }

            UndergrounderScript[] undergrounders = miniboss.GetComponentsInChildren<UndergrounderScript>(true);
            foreach (UndergrounderScript u in undergrounders)
            {
                if (u != null && u.isMiniboss)
                {
                    u.bossHealthBar = bossBar;
                }
            }
        }
        
        // Aktivuj tohoto minibosse
        if (miniboss != null)
            miniboss.SetActive(true);

        if (miniboss != null)
        {
            EnemyScript minibossEnemy = miniboss.GetComponentInChildren<EnemyScript>(true);
            if (minibossEnemy != null)
            {
                minibossEnemy.bossHealthBar = bossBar;
            }

            UndergrounderScript[] undergrounders = miniboss.GetComponentsInChildren<UndergrounderScript>(true);
            foreach (UndergrounderScript u in undergrounders)
            {
                if (u != null && u.isMiniboss)
                {
                    u.bossHealthBar = bossBar;
                }
            }
        }
        
        // Zamkni kameru
        PlayerScript.cameraLocked = true;
        
        // Aktivuj BossBar hned
        if (bossBar != null)
        {
            EnsureHierarchyActive(bossBar.transform);
            bossBar.gameObject.SetActive(true);
            bossBar.value = 1f;
            Debug.Log("BossBar aktivován z minibossScript");
        }
        else
        {
            Debug.LogWarning("minibossScript: objekt BossHealthBar nebyl nalezen.");
        }
        
        // Spustí bariéry
        AktivujBariery();
    }

    private Slider FindBossBarSlider()
    {
        Slider[] sliders = FindObjectsOfType<Slider>(true);
        foreach (Slider s in sliders)
        {
            if (s == null) continue;

            string path = GetPathLower(s.transform);
            if (path.Contains("pause") || path.Contains("volume") || path.Contains("settings"))
                continue;

            string n = s.gameObject.name.ToLowerInvariant();
            if (n == "bossbar" || n == "bosshealthbar" || n.Contains("boss"))
                return s;
        }

        return null;
    }

    private string GetPathLower(Transform t)
    {
        string path = "";
        while (t != null)
        {
            path += "/" + t.name;
            t = t.parent;
        }
        return path.ToLowerInvariant();
    }

    private void EnsureHierarchyActive(Transform t)
    {
        while (t != null)
        {
            t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ActivateBossFight();
        }
    }
}
