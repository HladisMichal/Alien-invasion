using UnityEngine;
using UnityEngine.UI;

public class Bulletscript : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float strelaSpeed = 50f; // Rychlost střely
    private Rigidbody2D rb; // Odkaz na Rigidbody2D střely
    private Collider2D bulletCollider;


    // Nastav v Inspectoru nebo najdi v Start()
    public GameObject skorebonusPrefab;

    void Start()
    {
        // Nastavení Z souřadnice střely, aby byla vždy viditelná

        // Nastavení rotace střely podle směru
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Získání Rigidbody2D a nastavení rychlosti
        rb = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();

        IgnorePlatformCollisions();

        if (rb != null)
        {
            rb.linearVelocity = direction * strelaSpeed; // Střela letí rovně ve směru, kam je otočená
        }
    }

    void OnBecameInvisible()
    {
        // Zničení střely, když opustí obrazovku
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            return;
        }

        // Kontrola, zda se střela dotkla objektu s tagem "Ground"
        if (collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject); // Zničení střely
        }
        if (collision.gameObject.CompareTag("Enemy"))
        {
            PlayerScript.akceSkore += 100;
            ShowSkoreBonus(collision.transform.position); // předání pozice nepřítele
            Destroy(collision.gameObject); // Zničení nepřítele
            Destroy(gameObject); // Zničení střely
            Debug.Log("Nepřítel byl zničen!"); // Debug message pro zničení nepřítele
        }
        if (collision.gameObject.CompareTag("miniboss"))
        {
            Destroy(gameObject); // Zničení střely
        }
    }

    void IgnorePlatformCollisions()
{
    if (bulletCollider == null)
    {
        return;
    }

    try
    {
        // Ignoruj Platform
        GameObject[] platforms = GameObject.FindGameObjectsWithTag("Platform");
        foreach (GameObject platform in platforms)
        {
            Collider2D[] platformColliders = platform.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D platformCollider in platformColliders)
            {
                Physics2D.IgnoreCollision(bulletCollider, platformCollider, true);
            }
        }
        
        // Ignoruj UFO layer
        int ufoLayer = LayerMask.NameToLayer("Ufo");
        Collider2D[] ufoColliders = FindObjectsOfType<Collider2D>();
        foreach (Collider2D ufoCollider in ufoColliders)
        {
            if (ufoCollider.gameObject.layer == ufoLayer)
            {
                Physics2D.IgnoreCollision(bulletCollider, ufoCollider, true);
            }
        }
    }
    catch (UnityException)
    {
        // Tag Platform nemusí být definovaný ve všech scénách
    }
}

    void ShowSkoreBonus(Vector3 enemyPosition)
    {
        if (skorebonusPrefab != null)
        {
            // Posuň bonus trochu nad nepřítele (např. o 1 jednotku nahoru)
            Vector3 bonusPos = enemyPosition + new Vector3(0, 1f, 0);
            Instantiate(skorebonusPrefab, bonusPos, Quaternion.identity);
            // Fadeout a pohyb nahoru řeší animace na prefab objektu
        }
    }

   
}