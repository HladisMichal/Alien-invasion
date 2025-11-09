using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class PlayerScript : MonoBehaviour
{
    public GameObject player;
    public Camera kamera;
    public float moveSpeed; 
    public float jumpForce;
    private Rigidbody2D rb;
    private bool isGrounded;
    private float scale;
    public GameObject strelaPrefab;
    private bool onPlatform;
    private Collider2D playerCollider;
    private Collider2D platformCollider;
    public int zivoty = 3; 
    public static double skore;
    public double pohyboveSkore;
    public static double akceSkore;
    public TMP_Text skoreText;
    public double maxskore;
    public UnityEngine.UI.Text GameOverText;
    public GameObject deathZone; 

    public Button restartButton;

    private Animator animator;

    public Image[] hearts; // nastav v inspektoru na 3 Image objekty v HeartsPanelu
    public Sprite heartFull; // nastav v inspektoru na obrázek plného srdce
    public Sprite heartEmpty;

    private float fireCooldown = 0.5f; // interval mezi střelami v sekundách
    private float lastFireTime = -999f;

    public Transform firePoint; // nastav v Inspectoru na FirePoint (child hráče)

    public float groundCheckDistance = 0.2f; // vzdálenost pro kontrolu pod hráčem
    public string[] groundTags = { "Ground", "Platform" }; // povolené tagy pro zem
    public float groundCheckWidth = 1f; // šířka boxu (přizpůsob šířce hráče)
    public float groundCheckHeight = 0.05f; // výška boxu (malá, těsně pod nohama)

    void Start()
    {
        UpdateHearts(); 
        scale = player.transform.localScale.x;
        if (player != null)
        {
            rb = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
            if (rb == null)
            {
                Debug.LogError("Player GameObject nemá připojený Rigidbody2D!");
            }
            else
            {
                rb.freezeRotation = true;
            }
            akceSkore = 0;
        }
        else
        {
            Debug.LogError("Není připojený GameObject player!");
        }

        if (player != null && kamera != null)
        {
            kamera.transform.position = new Vector3(player.transform.position.x, kamera.transform.position.y, kamera.transform.position.z);
        }
        animator = player.GetComponent<Animator>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            onPlatform = true;
            platformCollider = collision.collider; // Uložení reference na kolider platformy
        }
        if (collision.gameObject.CompareTag("Laser"))
        {
            OdeberZivoty();
            Debug.Log("Hráč se dotkl laseru! Životy: " + zivoty);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            onPlatform = false;
            // Necháme platformCollider nastavený, aby se kolize mohla obnovit
        }

    }

    void Update()
    {
        if (player != null && kamera != null)
        {
            if (zivoty <= 0)
            {
                Debug.Log("Konec hry! Hráč byl zničen.");
                if(GameOverText != null){
                GameOverText.gameObject.SetActive(true);
                GameOverText.text = "Konec hry!  Maximální skóre: " + Mathf.Round((float)maxskore);
                }
                if (restartButton != null)
                    restartButton.gameObject.SetActive(true);

                Destroy(player); 
                Time.timeScale = 0; 
                return;   
            }
            pohyboveSkore = player.transform.position.x - 341;
            skore = pohyboveSkore + akceSkore;
            if (skoreText != null)
            {
                skoreText.text = Mathf.Round((float)skore).ToString();
            }
            if (skore > maxskore)
        {
            maxskore = skore;
        }
            if (deathZone != null)
        {
            Vector3 pos = deathZone.transform.position;
            pos.x = player.transform.position.x;
            deathZone.transform.position = pos;
        }
        if(skoreText != null){
                skoreText.text = Mathf.Round((float)skore).ToString();
        }


            float move = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
            player.transform.Translate(move, 0, 0);

            if (animator != null)
            {
                animator.SetFloat("Speed", Mathf.Abs(move));
            }

            // Skákání přes dva Raycasty těsně NAD spodní hranou collideru (levý a pravý roh)
if (Input.GetButtonDown("Jump") && rb != null && GetIsGrounded() && Mathf.Abs(rb.velocity.y) < 0.01f)
{
    Jump();
}

            kamera.transform.position = new Vector3(player.transform.position.x, kamera.transform.position.y, kamera.transform.position.z);

            if (move > 0)
            {
                player.transform.localScale = new Vector3(scale, scale, 1);
            }
            else if (move < 0)
            {
                player.transform.localScale = new Vector3(-scale, scale, 1);
            }

            // Kontrola, zda je stisknuta klávesa "Control" pro průchod dolů
            if (Input.GetKeyDown(KeyCode.LeftControl) && onPlatform && platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, true); 
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl) && platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }

            if (Input.GetMouseButton(0))
{
    if (Time.time - lastFireTime >= fireCooldown)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (strelaPrefab != null && firePoint != null)
        {
            Vector3 spawnPos = firePoint.position;

            // Zjisti vzdálenost kamery od scény (většinou -10 pro 2D)
            float zDistance = Mathf.Abs(kamera.transform.position.z - spawnPos.z);

            // Převod myši na světové souřadnice ve stejné rovině jako firePoint
            Vector3 mouseWorld = kamera.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                zDistance
            ));
            mouseWorld.z = spawnPos.z; // sjednotíme Z

            Vector2 direction = (mouseWorld - spawnPos).normalized;

            GameObject bullet = Instantiate(strelaPrefab, spawnPos, Quaternion.identity);
            Bulletscript bs = bullet.GetComponent<Bulletscript>();
            bs.direction = direction;

            lastFireTime = Time.time;
        }
    }
}
        }

    }

    private bool GetIsGrounded()
{
    if (playerCollider == null) return false;
    Bounds bounds = playerCollider.bounds;
    float groundCheckDistance = 0.1f; // krátký paprsek těsně pod nohama

    // Raycasty začínají těsně POD spodní hranou collideru, blízko rohů
    float margin = bounds.size.x * 0.15f; // 15 % šířky od kraje
    float originY = bounds.min.y - 0.01f; // těsně pod spodní hranou

    Vector2 leftOrigin = new Vector2(bounds.min.x + margin, originY);
    Vector2 rightOrigin = new Vector2(bounds.max.x - margin, originY);

    RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDistance);
    RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDistance);

    foreach (var hit in new[] { hitLeft, hitRight })
    {
        if (hit.collider != null)
        {
            foreach (string tag in groundTags)
            {
                
                if (hit.collider.CompareTag(tag))
                {
                
                    return true;
                }
            }
        }
    }
    return false;
}


     void Jump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone"))
        {
            OdeberZivoty();
            Debug.Log("Hráč spadl do smrtící zóny! Životy: " + zivoty);

            // Najdi všechny objekty s tagem "Ground" a "Platform"
            GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
            GameObject[] platformObjects = GameObject.FindGameObjectsWithTag("Platform");

            // Spoj oba seznamy do jednoho
            List<GameObject> allSurfaces = new List<GameObject>();
            allSurfaces.AddRange(groundObjects);
            allSurfaces.AddRange(platformObjects);

            if (allSurfaces.Count == 0)
            {
                Debug.LogWarning("Nebyl nalezen žádný objekt s tagem 'Ground' nebo 'Platform'!");
                return;
            }

            // Najdi nejbližší objekt
            GameObject nearestSurface = null;
            float shortestDistance = Mathf.Infinity;

            foreach (GameObject surface in allSurfaces)
            {
                float distance = Vector2.Distance(player.transform.position, surface.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestSurface = surface;
                }
            }

            if (nearestSurface != null)
            {
                Vector3 safePosition = nearestSurface.transform.position;

                // Zkus získat BoxCollider2D a použij jeho výšku
                BoxCollider2D col = nearestSurface.GetComponent<BoxCollider2D>();
                float surfaceHeight = 1f;
                if (col != null)
                    surfaceHeight = col.size.y * nearestSurface.transform.localScale.y;
                else
                    surfaceHeight = nearestSurface.transform.localScale.y;

                safePosition.y += surfaceHeight / 2f + 1f; // 1f je rezerva nad povrchem
                player.transform.position = safePosition;

                Debug.Log("Hráč byl přesunut na bezpečné místo nad 'Ground' nebo 'Platform'.");
            }
            else
            {
                Debug.LogWarning("Nebyl nalezen žádný vhodný objekt s tagem 'Ground' nebo 'Platform'!");
            }

        }
        if (other.CompareTag("Collectible"))
    {
        if (zivoty < hearts.Length)
        {
            PridejZivoty();
            Destroy(other.gameObject);
        }
    
    }
    }
            public void UpdateHearts()
            {
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (i < zivoty)
                        hearts[i].sprite = heartFull;
                    else
                        hearts[i].sprite = heartEmpty;
                }
            }
            public void OdeberZivoty()
            {
                zivoty -= 1;
                UpdateHearts();
            }
            public void PridejZivoty()
            {
                    zivoty += 1;
                    UpdateHearts();
                
            }
           public void RestartGame()
            {
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
}

