using UnityEngine;

public class SpikesScript : MonoBehaviour
{
    public int damageAmount = 1; // Kolik života seberou
    
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Když hráč vstoupí do spiků
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerScript playerScript = other.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                playerScript.OdeberZivoty();
                Debug.Log("Spiky způsobily damage! Životy hráče: " + playerScript.zivoty);
            }
            
            // Zničit spiky téměř hned
            DestroySpikes();
        }
        
    }

    private void DestroySpikes()
    {
        Destroy(gameObject);
    }
}
