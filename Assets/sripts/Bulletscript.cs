using UnityEngine;
using UnityEngine.UI;

public class Bulletscript : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float strelaSpeed = 50f; // Rychlost střely
    private Rigidbody2D rb; // Odkaz na Rigidbody2D střely


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
        if (rb != null)
        {
            rb.velocity = direction * strelaSpeed; // Střela letí rovně ve směru, kam je otočená
        }
    }

    void OnBecameInvisible()
    {
        // Zničení střely, když opustí obrazovku
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
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