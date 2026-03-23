using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

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