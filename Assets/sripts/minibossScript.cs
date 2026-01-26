using UnityEngine;

public class minibossScript : MonoBehaviour
{
    public int druh;
    public GameObject miniboss;
    public UFOFollowPlayer[] hranice; // UFO pro bariéry arény
    
    private bool bossFightActive = false;
    private bool fightAlreadyStarted = false; // Aby se fight spustil jen jednou

    void Update()
    {
        if (bossFightActive)
        {
            if (druh == 1)
            {
                
                
            }
            else if (druh == 2)
            {
                
            }
            else if (druh == 3)
            {
            
            }
        }
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
        
        // Aktivuj tohoto minibosse
        if (miniboss != null)
            miniboss.SetActive(true);
        
        // Zamkni kameru
        PlayerScript.cameraLocked = true;
        
        // Aktivuj BossBar hned
        Slider bossBar = FindObjectOfType<Slider>(true);
        if (bossBar != null)
        {
            bossBar.gameObject.SetActive(true);
            bossBar.value = 1f;
            Debug.Log("BossBar aktivován z minibossScript");
        }
        
        // Spustí bariéry
        AktivujBariery();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ActivateBossFight();
        }
    }
}
