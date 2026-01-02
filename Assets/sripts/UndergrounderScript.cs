using UnityEngine;
using System.Collections;

public class UndergrounderScript : MonoBehaviour
{
    public GameObject partToActivate; // část prefabu, kterou chceš aktivovat
    public Transform holeTransform;   // child objekt "hole"
    public Transform playerTransform; // referenci na hráče

    public float activationDistance = 9.6f; // vzdálenost pro aktivaci
    public float waitTime = 5f; // čas čekání po aktivaci

    private bool canActivate = true; // může se aktivovat?
    public BoxCollider2D damageCollider; // přiřaď v Inspectoru BoxCollider, který má způsobovat damage
    
    private float lastDamageTime = -100f; // čas posledního damage
    public float damageCooldown = 20f; // cooldown mezi damage (1 sekunda)

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
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
                    StartCoroutine(WaitBeforeNextActivation()); // spustí čekání hned po aktivaci
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
        if (other.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
        {
            Debug.Log("Undergrounder se dotkl hráče - odebírám život!");
            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.OdeberZivoty();
                lastDamageTime = Time.time;
            }
        }
    }

    // Kontinuální kontrola - volá se každý frame když jsou v kontaktu
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
        {
            Debug.Log("Undergrounder stále v kontaktu s hráčem - odebírám život!");
            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.OdeberZivoty();
                lastDamageTime = Time.time;
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
            Debug.Log("Collidery se dotýkají, odebrání životů.");
            PlayerScript player = playerTransform.GetComponent<PlayerScript>();
            if (player != null)
            {
                Debug.Log("Odebírám životy hráči.");
                player.OdeberZivoty();
            }
        }
        else
        {
            Debug.Log("Collidery se nedotýkají.");
        }
    }
}
