using UnityEngine;
using UnityEngine.UI;

public class EnemyScript : MonoBehaviour
{
    public bool isMiniboss = false; // Určuje, zda je to miniboss
    public int normalHealth = 1; // Životy normálního nepřítele
    public int minibossHealth = 10; // Životy minibosse
    private int currentHealth;
    
    public GameObject enemyProjectilePrefab; // Prefab střely nepřítele
    public float fireRate = 4f; // Interval mezi střelami (v sekundách)
    public float minibossFireRate = 5f; // Interval mezi střelami minibosse (v sekundách)
    private Transform player; // Odkaz na hráče
    private bool isVisible = false; // Kontrola, zda je nepřítel viditelný
    private float nextFireTime = 0f; // Čas, kdy může nepřítel znovu střílet
    public static double skore;
    public GameObject ufo_prefab;
    
    // Boss bar UI
    public Slider bossHealthBar; // Slider pro zdraví minibosse - přiřaď ručně v Inspectoru z Canvas ve scéně
    
    // Bonus drop
    public GameObject bonusHeartPrefab; // Prefab srdíčka které se dropne po smrti minibosse
    
    // Miniboss specifické vlastnosti
    public Transform[] teleportPositions; // 6 přednastavených pozic pro teleport
    public float teleportCooldown = 5f; // Interval mezi teleporty
    public float minDistanceFromPlayer = 0.2f; // Minimální vzdálenost od hráče při teleportu
    private float nextTeleportTime = 0f;
    private int lastTeleportIndex = -1; // Poslední použitá pozice
    private float scale;

    void Start()
    {
        // Najdi hráče podle tagu "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        // Nastav životy podle typu
        currentHealth = isMiniboss ? minibossHealth : normalHealth;
        scale = transform.localScale.x;
        
        // Nastav boss bar - najdi Slider ve scéně
        if (isMiniboss)
        {
            if (bossHealthBar == null)
            {
                // Najdi jakýkoliv Slider ve scéně (měl by být jen jeden - BossBar)
                bossHealthBar = FindObjectOfType<Slider>(true); // true = include inactive
            }
            
            if (bossHealthBar != null)
            {
                bossHealthBar.value = 1f;
                bossHealthBar.gameObject.SetActive(false); // Skryj dokud není aktivní
            }
        }
    }

    void Update()
    {
        // Otáčení minibosse za hráčem
        if (isMiniboss && player != null)
        {
            float direction = player.position.x - transform.position.x;
            if (direction < 0)
            {
                transform.localScale = new Vector3(scale, scale, 1);
            }
            else if (direction > 0)
            {
                transform.localScale = new Vector3(-scale, scale, 1);
            }
        }

        // Pokud je nepřítel viditelný a je čas střílet
        if (isVisible && Time.time >= nextFireTime)
        {
            Shoot();
            float currentFireRate = isMiniboss ? minibossFireRate : fireRate;
            nextFireTime = Time.time + currentFireRate; // Nastav čas další střelby
        }
        
        // Teleportování minibosse
        if (isMiniboss && isVisible && Time.time >= nextTeleportTime && teleportPositions != null && teleportPositions.Length > 0)
        {
            Teleport();
            nextTeleportTime = Time.time + teleportCooldown;
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
        
        // Zobraz boss bar když se miniboss stane viditelným
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.value = 1f;
            bossHealthBar.gameObject.SetActive(true);
        }
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
                playerScript.OdeberZivoty(); // Odebere životy hráči (a automaticky spustí invincibility)
                Debug.Log("Hráč dostal hit! Životy: " + playerScript.zivoty);
            }
        }
        if (collision.gameObject.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Debug.Log("Nepřítel byl zasažen! Životy: " + currentHealth);
        }
    }
    
    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Aktualizuj boss bar
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.value = currentHealth / (float)minibossHealth;
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Nepřítel byl zničen!");
        
        // Vypni collider aby se neumožnily další kolize
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        
        // Schovej boss bar
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }
        
        // Pokud je to miniboss, zavolej deaktivaci bariér
        if (isMiniboss)
        {
            minibossScript mbScript = FindObjectOfType<minibossScript>();
            if (mbScript != null)
            {
                mbScript.DeaktivujBariery();
            }
            
            // Spawn bonusové srdíčko na místě minibosse
            if (bonusHeartPrefab != null)
            {
                Instantiate(bonusHeartPrefab, transform.position, Quaternion.identity);
            }
        }
        
        Destroy(gameObject);
    }
    
    void Teleport()
    {
        if (teleportPositions.Length == 0 || player == null) return;
        
        // Najdi validní pozici (ne stejná jako poslední a ne příliš blízko hráče)
        int attempts = 0;
        int maxAttempts = 20; // Aby se nezaseklo v nekonečném loopu
        int newIndex;
        
        do
        {
            newIndex = Random.Range(0, teleportPositions.Length);
            attempts++;
            
            // Pokud nemáme žádnou validní pozici po mnoha pokusech, vezmi jakoukoliv
            if (attempts >= maxAttempts)
            {
                break;
            }
            
            // Kontrola: není stejná jako poslední A není příliš blízko hráče
            if (newIndex != lastTeleportIndex && teleportPositions[newIndex] != null)
            {
                float distanceToPlayer = Vector2.Distance(teleportPositions[newIndex].position, player.position);
                if (distanceToPlayer >= minDistanceFromPlayer)
                {
                    break; // Našli jsme validní pozici
                }
            }
            
        } while (attempts < maxAttempts);
        
        lastTeleportIndex = newIndex;
        
        if (teleportPositions[newIndex] != null)
        {
            transform.position = teleportPositions[newIndex].position;
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
        float bulletSpeed = 2.5f + (float)(skore / 10000.0); // každých 1000 skóre +1f k rychlosti střely
        bulletSpeed = Mathf.Clamp(bulletSpeed, 10f, 20f);

        // Přidej offset pro spawn střely
        
        GameObject projectile = Instantiate(enemyProjectilePrefab, transform.position, Quaternion.identity);

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