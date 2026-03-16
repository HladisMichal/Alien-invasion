using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private void Awake()
{
    Debug.Log("Probouzím MusicManager na objektu: " + gameObject.name);

    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("Instance byla null. Nastavuji tuto jako hlavní: " + gameObject.name);
    }
    else
    {
        Debug.Log("Instance už je obsazená objektem: " + Instance.name + ". Mažu tento objekt: " + gameObject.name);
        Destroy(gameObject);
    }
}

    // Tato funkce zajistí, že se starý manažer správně "odhlásí" před smazáním
    public static void StopAllMusic()
    {
        if (Instance != null)
        {
            GameObject oldMusic = Instance.gameObject;
            Instance = null; // Nejdřív vynulujeme referenci
            Destroy(oldMusic); // Pak smažeme objekt
        }
    }
}