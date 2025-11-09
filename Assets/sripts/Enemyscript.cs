using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public GameObject enemyProjectilePrefab; // Prefab střely nepřítele
    public float fireRate = 4f; // Interval mezi střelami (v sekundách)
    private Transform player; // Odkaz na hráče
    private bool isVisible = false; // Kontrola, zda je nepřítel viditelný
    private float nextFireTime = 0f; // Čas, kdy může nepřítel znovu střílet
    public static double skore;
    public GameObject ufo_prefab;

    void Start()
    {
        // Najdi hráče podle tagu "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        

        // Pokud je nepřítel viditelný a je čas střílet
        if (isVisible && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate; // Nastav čas další střelby
        }

        // Kontrola vzdálenosti od hráče pro zničení
        if (player != null)
        {
            PlayerScript.skore = skore;
            float distance = Vector2.Distance(transform.position, player.position);
        }
    }

    void OnBecameVisible()
    {
        // Nepřítel je viditelný na obrazovce
        isVisible = true;
    }

    void OnBecameInvisible()
    {
        // Nepřítel není viditelný na obrazovce
        isVisible = false;
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Kontrola kolize s hráčem
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerScript playerScript = collision.gameObject.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                playerScript.OdeberZivoty();
                Debug.Log("Hráč dostal hit! Životy: " + playerScript.zivoty);
            }
        }
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Debug.Log("Nepřítel byl zničen!");
            Destroy(gameObject);
        }
    }

    void Shoot()
    {
        if (player == null)
        {
            Debug.LogWarning("Player není přiřazen!");
            return;
        }
        double skore = PlayerScript.skore;
        float bulletSpeed = 40f + (float)(skore / 1000.0); // každých 1000 skóre +1f
        bulletSpeed = Mathf.Clamp(bulletSpeed, 40f, 80f);

        // Přidej offset pro spawn střely
        Vector3 spawnOffset = new Vector3(-5f, 5f, 0); // uprav hodnoty podle potřeby
        GameObject projectile = Instantiate(enemyProjectilePrefab, transform.position + spawnOffset, Quaternion.identity);

        Vector2 direction = (player.position - transform.position).normalized;
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log("rychlost střely: " + bulletSpeed);
            rb.linearVelocity = direction * bulletSpeed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
    
}