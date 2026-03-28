using UnityEngine;
using UnityEngine.UI;

public class EnemyScript : MonoBehaviour
{
    public bool isMiniboss = false; 
    public int normalHealth = 1; 
    public int minibossHealth = 10; 
    private int currentHealth;
    
    public GameObject enemyProjectilePrefab; 
    public float fireRate = 4f; 
    public float minibossFireRate = 5f; 
    private Transform player; 
    private bool isVisible = false; 
    private float nextFireTime = 0f; 
    public static double skore;
    public GameObject ufo_prefab;
    
    public Slider bossHealthBar; 
    
    public GameObject bonusHeartPrefab; 
    
    public Transform[] teleportPositions; 
    public float teleportCooldown = 5f; 
    public float minDistanceFromPlayer = 0.2f; 
    private float nextTeleportTime = 0f;
    private int lastTeleportIndex = -1; 
    private float scale;
    public GameObject skoreBonusANI; 
    [Range(0f, 1f)] public float enemyFireSfxVolume = 1f;

    void Start()
    {
       
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        
        currentHealth = isMiniboss ? minibossHealth : normalHealth;
        scale = transform.localScale.x;
        
        
        if (isMiniboss)
        {
            if (bossHealthBar == null)
            {
                
                bossHealthBar = FindObjectOfType<Slider>(true); 
            }
            
            if (bossHealthBar != null)
            {
                bossHealthBar.value = 1f;
                bossHealthBar.gameObject.SetActive(false); 
            }
        }
    }

    void Update()
    {
        
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

        if (isVisible && Time.time >= nextFireTime)
        {
            Shoot();
            float currentFireRate = isMiniboss ? minibossFireRate : fireRate;
            nextFireTime = Time.time + currentFireRate;
        }
        
        
        if (isMiniboss && isVisible && Time.time >= nextTeleportTime && teleportPositions != null && teleportPositions.Length > 0)
        {
            Teleport();
            nextTeleportTime = Time.time + teleportCooldown;
        }

        
        // Skóre řídí PlayerScript; tady ho nepřepisujeme.
    }

    void OnBecameVisible()
    {
        
        isVisible = true;
        
        
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.value = 1f;
            bossHealthBar.gameObject.SetActive(true);
        }
    }

    void OnBecameInvisible()
    {
       
        isVisible = false;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        
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
            TakeDamage(1);
            Debug.Log("Nepřítel byl zasažen! Životy: " + currentHealth);
        }
    }
    
    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        
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
        
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        
        
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }
        
        
        if (isMiniboss)
        {
            minibossScript mbScript = FindObjectOfType<minibossScript>();
            if (mbScript != null)
            {
                mbScript.DeaktivujBariery();
                Instantiate(skoreBonusANI, new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), Quaternion.identity);
                PlayerScript.akceSkore += 200;            }
            
            
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
        
        int attempts = 0;
        int maxAttempts = 20; 
        int newIndex;
        
        do
        {
            newIndex = Random.Range(0, teleportPositions.Length);
            attempts++;
            
            if (attempts >= maxAttempts)
            {
                break;
            }
            
            if (newIndex != lastTeleportIndex && teleportPositions[newIndex] != null)
            {
                float distanceToPlayer = Vector2.Distance(teleportPositions[newIndex].position, player.position);
                if (distanceToPlayer >= minDistanceFromPlayer)
                {
                    break; 
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
        float bulletSpeed = 2.5f + (float)(skore / 10000.0); 
        bulletSpeed = Mathf.Clamp(bulletSpeed, 10f, 20f);

        
        GameObject projectile = Instantiate(enemyProjectilePrefab, transform.position, Quaternion.identity);

        if (SFXManagerScript.Instance != null)
        {
            SFXManagerScript.Instance.PlaySFX(SFXManagerScript.SfxId.EnemyFire, enemyFireSfxVolume);
        }

        Vector2 direction = (player.position - transform.position).normalized;
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
    
}