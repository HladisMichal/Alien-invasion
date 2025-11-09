using UnityEngine;

public class EnemyBulletscript : MonoBehaviour
{
    private Rigidbody2D rb; // Reference to the bullet's Rigidbody2D
    private Vector2 targetPosition; // Fixed Z coordinate for the bullet (closer to the camera)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetTarget(Vector3 target)
    {
        // Nastav cílovou pozici střely
        targetPosition = target;
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject); // Zničí projektil, když se stane neviditelným

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerScript Playerscript = collision.gameObject.GetComponent<PlayerScript>();
            Playerscript.OdeberZivoty(); // Odebere životy hráči
            Debug.Log("životy: " + Playerscript.zivoty);
            Destroy(gameObject); // Zničí projektil
        }
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Střela zasáhla zem!");
            Destroy(gameObject); // Zničí projektil při kolizi se zemí nebo platformou
        }
    }
}

