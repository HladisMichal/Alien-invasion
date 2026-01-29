using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UndergrounderScript : MonoBehaviour
{
    public GameObject partToActivate; // část prefabu, kterou chceš aktivovat
    public Transform holeTransform;   // child objekt "hole"
    public Transform playerTransform; // referenci na hráče

    public float activationDistance = 9.6f; // vzdálenost pro aktivaci
    public float waitTime = 5f; // čas čekání po aktivaci

    private bool canActivate = true; // může se aktivovat?
    public BoxCollider2D damageCollider; // přiřaď v Inspektoru BoxCollider, který má způsobovat damage
    
    private float lastDamageTime = -100f; // čas posledního damage
    public float damageCooldown = 0.5f; // cooldown mezi damage
    
    // Boss Bar
    public bool isMiniboss = false;
    public int minibossHealth = 10;
    private int currentHealth;
    public Slider bossHealthBar;
    public GameObject bonusHeartPrefab; // Bonus při smrti
    
    private bool isVisible = false;
    
    // Miniboss emergence mechanic
    public Transform[] spawnPoints; // GameObjecty kde se může spawnout
    public GameObject warningIndicatorPrefab; // Vykřičník prefab
    public float warningDuration = 0.25f; // Jak dlouho se má čekat než se spawne miniboss
    public Transform mainUndergrounderParent; // Hlavní parent GameObject který se má přesouvat
    public Transform undergrounderSprite; // Child GameObject s animací spriteu
    private Animator animator;
    private Animator spriteAnimator; // Animator na undergroundersprite
    private Vector3 offsetFromParentToHole; // Rozdíl mezi parentem a hole

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Pokud není nastaven parent, použij transform tohoto objektu
        if (mainUndergrounderParent == null)
            mainUndergrounderParent = transform;
        
        // Zapamatuj si rozdíl mezi parentem a hole (pokud existuje)
        if (holeTransform != null)
            offsetFromParentToHole = holeTransform.position - mainUndergrounderParent.position;
        
        animator = GetComponent<Animator>();
        
        // Najdi animator na undergroundersprite
        if (undergrounderSprite != null)
        {
            spriteAnimator = undergrounderSprite.GetComponent<Animator>();
        }
        
        // Boss bar setup - POUZE na hlavním undergrounder parent objektu
        if (mainUndergrounderParent == transform)
        {
            currentHealth = isMiniboss ? minibossHealth : 1;
            if (bossHealthBar == null)
            {
                bossHealthBar = FindObjectOfType<Slider>(true);
            }
            
            if (bossHealthBar != null && isMiniboss)
            {
                bossHealthBar.value = 1f;
                bossHealthBar.gameObject.SetActive(true);
                // Aktivuj i parent Canvas pokud je deaktivovaný
                if (bossHealthBar.transform.parent != null)
                {
                    bossHealthBar.transform.parent.gameObject.SetActive(true);
                }
            }
        }
    }

    void Update()
    {
        // Boss bar - pořád viditelný během hry - JEN na hlavním undergrounder parent
        if (isMiniboss && bossHealthBar != null && isVisible && mainUndergrounderParent == transform)
        {
            bossHealthBar.value = currentHealth / (float)minibossHealth;
        }
        
        if (canActivate && holeTransform != null && playerTransform != null)
        {
            float distance = Vector2.Distance(
                new Vector2(holeTransform.position.x, holeTransform.position.y),
                new Vector2(playerTransform.position.x, playerTransform.position.y)
            );
            
            if (distance < activationDistance)
            {
                if (partToActivate != null)
                {
                    partToActivate.SetActive(true);
                    isVisible = true;
                    
                    // Pro minibossa zapni damage collider hned na začátku emergence
                    if (isMiniboss && damageCollider != null)
                        damageCollider.enabled = true;
                    
                    StartCoroutine(WaitBeforeNextActivation());
                }
            }
        }
    }

    // Veřejná metoda pro deaktivaci části prefabu
    public void DeactivatePart()
    {
        if (partToActivate != null)
            partToActivate.SetActive(false);
    }

    private IEnumerator WaitBeforeNextActivation()
    {
        canActivate = false; // zakáže aktivaci
        yield return new WaitForSeconds(waitTime); // počká
        canActivate = true; // povolí aktivaci
    }

    // Automatická detekce kolize - volá se okamžitě při dotyku
    void OnTriggerEnter2D(Collider2D other)
    {
        // Pokud je script na child objektu a parent má taky script, nespustí se zde
        if (transform.parent != null && transform.parent.GetComponent<UndergrounderScript>() != null)
        {
            // ALE PRO PROJEKTILY POKRAČUJEME
            if (!other.CompareTag("Projectile"))
                return;
        }
        
        if (other.CompareTag("Player"))
        {
            // Zkontroluj cooldown - aby se damage dal jen jednou
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerScript player = other.GetComponent<PlayerScript>();
                if (player != null)
                {
                    player.OdeberZivoty();
                    lastDamageTime = Time.time;
                }
            }
        }
        
        // JEN miniboss dostává damage od projektilů
        if (other.CompareTag("Projectile"))
        {
            // Najdi parent script pokud existuje
            UndergrounderScript targetScript = this;
            if (transform.parent != null)
            {
                UndergrounderScript parentScript = transform.parent.GetComponent<UndergrounderScript>();
                if (parentScript != null)
                {
                    if (parentScript.isMiniboss)
                        targetScript = parentScript;
                }
            }
            
            // Zpracuj damage jen pokud je miniboss a jsme na hlavním undergrounder parent objektu
            if (targetScript.isMiniboss && targetScript.mainUndergrounderParent == targetScript.transform)
            {
                targetScript.TakeDamage(1);
                Destroy(other.gameObject); // Zniči projektil
            }
        }
    }

    // Záložní metoda pro manuální volání (pokud je používána v animaci)
    public void uberZivot()
    {
        if (playerTransform == null || damageCollider == null)
        {
            Debug.LogWarning("Player transform or damage collider not assigned.");
            return;
        }

        // Zkontroluj, jestli se damage collider dotýká hráčova collideru
        Collider2D playerCollider = playerTransform.GetComponent<Collider2D>();
        if (playerCollider != null && damageCollider.IsTouching(playerCollider))
        {
            PlayerScript player = playerTransform.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.OdeberZivoty();
            }
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
        
        lastDamageTime = Time.time;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        // Skryj boss bar
        if (isMiniboss && bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }
        
        // Deaktivuj část
        DeactivatePart();
        
        // Ukončí boss fight (vrátí kameru a deaktivuje UFO)
        if (isMiniboss)
        {
            minibossScript miniboss = FindObjectOfType<minibossScript>();
            if (miniboss != null)
            {
                miniboss.DeaktivujBariery();
            }
        }
        
        // Spawn bonus
        if (isMiniboss && bonusHeartPrefab != null)
        {
            Instantiate(bonusHeartPrefab, new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    
    // === MINIBOSS ANIMATION EVENTS ===
    
    // Zavolej z Animation Event na konci Emerge animace
    public void OnMinibossEmergeComplete()
    {
        if (damageCollider != null)
            damageCollider.enabled = true;
    }
    
    // Zavolej z Animation Event na konci vulnerable fáze (před Hide)
    public void OnMinibossVulnerableEnd()
    {
        if (damageCollider != null)
            damageCollider.enabled = false;
    }
    
    // Zavolaj z Animation Event na konci Hide animace
    public void OnMinibossHideComplete()
    {
        StartCoroutine(ReappearAfterHide());
    }
    
    private IEnumerator ReappearAfterHide()
    {
        // 1. Přesuň doleva o 50 od hráče (místo -500)
        Vector3 originalPos = mainUndergrounderParent.position;
        float originalZ = originalPos.z; // Zapamatuj si původní Z
        mainUndergrounderParent.position = new Vector3(playerTransform.position.x - 50f, originalPos.y, originalZ);
        isVisible = false;
        
        // 2. Počkej náhodně mezi 1-3 sekundami
        float randomWait = Random.Range(1f, 3f);
        yield return new WaitForSeconds(randomWait);
        
        // 3. Zjisti finální spawn pozici
        Vector3 spawnPos = originalPos;
        if (playerTransform != null)
        {
            // Pokud máš spawn points, vezmi nejbližší k hráči
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform closestPoint = spawnPoints[0];
                float minDistance = Vector2.Distance(playerTransform.position, closestPoint.position);
                
                foreach (Transform point in spawnPoints)
                {
                    float distance = Vector2.Distance(playerTransform.position, point.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = point;
                    }
                }
                
                // Spawn point pozice - offset, ZACHOVEJ PŮVODNÍ Z
                Vector3 newPos = closestPoint.position - offsetFromParentToHole;
                spawnPos = new Vector3(newPos.x, newPos.y, originalZ);
            }
            else
            {
                // Jinak přímo pod hráčem - jen X, zachovej původní Y a Z
                spawnPos = new Vector3(
                    playerTransform.position.x - offsetFromParentToHole.x,
                    originalPos.y,
                    originalZ
                );
            }
        }
        
        // 4. Spawni vykřičník na finální pozici
        GameObject warningInstance = null;
        if (warningIndicatorPrefab != null)
        {
            warningInstance = Instantiate(warningIndicatorPrefab, new Vector3(spawnPos.x, spawnPos.y + 1f, spawnPos.z), Quaternion.identity);
        }
        
        // 5. Počkej warning dobu
        yield return new WaitForSeconds(warningDuration);
        
        // 6. Smaž vykřičník
        if (warningInstance != null)
            Destroy(warningInstance);
        
        // 7. Teleportuj minibossa na finální pozici
        mainUndergrounderParent.position = spawnPos;
        
        // 8. Restart animace od začátku
        if (animator != null)
            animator.Play(0, -1, 0f);
        
        if (spriteAnimator != null)
            spriteAnimator.Play(0, -1, 0f);
        
        isVisible = true;
    }
    
}
