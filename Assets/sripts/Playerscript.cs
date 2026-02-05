using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.Tilemaps;

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
    public static bool cameraLocked = false; // Lock kamery během boss fightu
    public TMP_Text skoreText;
    public double maxskore;
    public UnityEngine.UI.Text GameOverText;
    public GameObject deathZone; 

    public Button restartButton;
    public Button exitButton;
    
    public Tilemap groundTilemap; // Volitelné: jedna Tilemap (když je nastavena, použije se přímo)
    public bool autoFindTilemaps = true; // Když není nastavena, najdeme Tilemapy automaticky ve scéně
    public string[] tilemapTags = { "Ground", "Platform" }; // Volitelná filtrace Tilemap podle tagu
    public float tileSearchRadius = 50f; // Poloměr hledání bezpečného tile (v jednotkách světa)

    private Animator animator;

    public Image[] hearts; // nastav v inspektoru na 3 Image objekty v HeartsPanelu
    public Sprite heartFull; // nastav v inspektoru na obrázek plného srdce
    public Sprite heartEmpty;

    private float fireCooldown = 0.5f; // interval mezi střelami v sekundách
    private float lastFireTime = -999f;

    public Transform firePoint; // nastav v Inspectoru na FirePoint (child hráče)

    private Vector2 lastAimDirection = Vector2.right;

    public SpriteRenderer playerSprite;

    public Vector2 firePointRight = new Vector2(0.6f, 0.1f);
    public Vector2 firePointLeft = new Vector2(-0.6f, 0.1f);
    public Vector2 firePointUp = new Vector2(0f, 0.7f);
    public Vector2 firePointDown = new Vector2(0f, -0.4f);
    public Vector2 firePointUpRight = new Vector2(0.5f, 0.5f);
    public Vector2 firePointUpLeft = new Vector2(-0.5f, 0.5f);
    public Vector2 firePointDownRight = new Vector2(0.5f, -0.3f);
    public Vector2 firePointDownLeft = new Vector2(-0.5f, -0.3f);

    // Dash systém
    public float dashDistance = 25f; // vzdálenost dashe v jednotkách - upravitelná v Inspectoru  
    public float dashSpeed = 30f; // rychlost dashe - upravitelná v Inspectoru
    private float dashCooldown = 2f; // cooldown 2 sekundy
    private float lastDashTime = -999f;
    private bool isDashing = false;
    private Vector3 dashTarget;
    private Vector3 dashStart;

    // Ikona dashe (ready/cooldown)
    public Image dashIcon;
    public Sprite dashReadySprite;
    public Sprite dashCooldownSprite;

    public float groundCheckDistance = 0.2f; // vzdálenost pro kontrolu pod hráčem
    public string[] groundTags = { "Ground", "Platform" }; // povolené tagy pro zem
    public float groundCheckWidth = 1f; // šířka boxu (přizpůsob šířce hráče)
    public float groundCheckHeight = 0.05f; // výška boxu (malá, těsně pod nohama)

    void Start()
    {
        cameraLocked = false; // Reset camera lock při startu
        UpdateHearts(); 
        scale = player.transform.localScale.x;
        lastAimDirection = scale >= 0 ? Vector2.right : Vector2.left;
        if (playerSprite == null && player != null)
        {
            playerSprite = player.GetComponentInChildren<SpriteRenderer>();
        }
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
                if (exitButton != null)
                    exitButton.gameObject.SetActive(true);

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


            bool verticalAimHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
            float moveInput = verticalAimHeld ? 0f : (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float move = moveInput * moveSpeed * Time.deltaTime;
            player.transform.Translate(move, 0, 0);

            if (animator != null)
            {
                animator.SetFloat("Speed", Mathf.Abs(moveInput));
            }

            // Skákání přes dva Raycasty těsně NAD spodní hranou collideru (levý a pravý roh)
if (Input.GetButtonDown("Jump") && rb != null && GetIsGrounded() && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
{
    Jump();
}

            // Posun kamery - jen pokud není locknutá
            if (!cameraLocked)
            {
                kamera.transform.position = new Vector3(player.transform.position.x, kamera.transform.position.y, kamera.transform.position.z);
            }

            if (move > 0)
            {
                if (lastAimDirection.x == 0 && lastAimDirection.y == 0)
                    lastAimDirection = Vector2.right;
                if (playerSprite != null)
                    playerSprite.flipX = false;
            }
            else if (move < 0)
            {
                if (lastAimDirection.x == 0 && lastAimDirection.y == 0)
                    lastAimDirection = Vector2.left;
                if (playerSprite != null)
                    playerSprite.flipX = true;
            }

            UpdateAimDirection();

            // Kontrola, zda je stisknuta klávesa "Control" pro průchod dolů
            if (Input.GetKeyDown(KeyCode.LeftControl) && onPlatform && platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, true); 
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl) && platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }

            // Dash systém - detekce Shift klávesy
            if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time - lastDashTime >= dashCooldown && !isDashing)
            {
                StartDash();
            }
            

            // Provádění dashe
            if (isDashing)
            {
                UpdateDash();
            }

            // Přepnutí ikony a fill amount podle dostupnosti dashe
            bool dashReady = Time.time - lastDashTime >= dashCooldown && !isDashing;
            UpdateDashUI(dashReady, Time.time - lastDashTime);

            if (Input.GetMouseButton(0))
{
    if (Time.time - lastFireTime >= fireCooldown)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (strelaPrefab != null && firePoint != null)
        {
            Vector3 spawnPos = firePoint.position;

            Vector2 direction = lastAimDirection.normalized;
            if (direction == Vector2.zero)
                direction = Vector2.right;

            GameObject bullet = Instantiate(strelaPrefab, spawnPos, Quaternion.identity);
            Bulletscript bs = bullet.GetComponent<Bulletscript>();
            bs.direction = direction;

            lastFireTime = Time.time;
        }
    }
}
        }

    }

    void UpdateAimDirection()
    {
        int dirX = 0;
        int dirY = 0;
        if (Input.GetKey(KeyCode.D)) dirX += 1;
        if (Input.GetKey(KeyCode.A)) dirX -= 1;
        if (Input.GetKey(KeyCode.W)) dirY += 1;
        if (Input.GetKey(KeyCode.S)) dirY -= 1;

        Vector2 aimInput = new Vector2(dirX, dirY);
        if (aimInput != Vector2.zero)
        {
            lastAimDirection = aimInput.normalized;
            if (firePoint != null)
            {
                firePoint.right = new Vector3(lastAimDirection.x, lastAimDirection.y, 0f);
            }
            ApplyAimOffsets(dirX, dirY);
            if (animator != null)
            {
                float animMoveX = Mathf.Abs(dirX);
                animator.SetFloat("MoveX", animMoveX);
                animator.SetFloat("MoveY", dirY);
            }
            if (playerSprite != null && dirX != 0)
            {
                playerSprite.flipX = dirX < 0;
            }
        }
    }

    void ApplyAimOffsets(int dirX, int dirY)
    {
        if (firePoint != null)
        {
            Vector2 offset = firePointRight;
            if (dirX > 0 && dirY == 0) offset = firePointRight;
            else if (dirX < 0 && dirY == 0) offset = firePointLeft;
            else if (dirX == 0 && dirY > 0) offset = firePointUp;
            else if (dirX == 0 && dirY < 0) offset = firePointDown;
            else if (dirX > 0 && dirY > 0) offset = firePointUpRight;
            else if (dirX < 0 && dirY > 0) offset = firePointUpLeft;
            else if (dirX > 0 && dirY < 0) offset = firePointDownRight;
            else if (dirX < 0 && dirY < 0) offset = firePointDownLeft;

            if (dirX == 0 && dirY != 0)
            {
                if (playerSprite != null && playerSprite.flipX)
                {
                    offset.x = -offset.x;
                }
            }

            firePoint.localPosition = new Vector3(offset.x, offset.y, firePoint.localPosition.z);
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

            // Pokus o teleport na Tilemapy ve scéně (univerzálně, bez seznamu GameObjectů)
            Tilemap[] tilemapsToUse = GetRelevantTilemaps();
            if (tilemapsToUse != null && tilemapsToUse.Length > 0)
            {
                Vector3 safePos = FindNearestSafeTileAcrossTilemaps(player.transform.position, tilemapsToUse);
                if (safePos != Vector3.zero)
                {
                    player.transform.position = safePos;
                    Debug.Log("Hráč teleportován na bezpečný tile: " + safePos);
                    return;
                }
            }

            // Fallback na staré GameObjecty pokud Tilemap není přiřazená nebo nebyl nalezen tile
            GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
            GameObject[] platformObjects = GameObject.FindGameObjectsWithTag("Platform");

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
                skore = 0;
                akceSkore = 0;
                Time.timeScale = 1;
                cameraLocked = false; // Unlock kamery při restartu
                if (MapGeneration.instance != null)
                {
                    MapGeneration.instance.ResetMilestonesForRestart();
                }
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            void StartDash()
            {
                if (player == null) return;

                // Určí směr dashe podle toho, kam se hráč dívá
                float direction = player.transform.localScale.x > 0 ? 1f : -1f;

                // Nastavení start a cílové pozice
                dashStart = player.transform.position;
                dashTarget = dashStart;
                dashTarget.x += direction * dashDistance;

                // Spuštění dashe
                isDashing = true;
                lastDashTime = Time.time;

                Debug.Log("Dash začíná! Směr: " + (direction > 0 ? "doprava" : "doleva") + ", vzdálenost: " + dashDistance);
            }

            void UpdateDash()
            {
                if (!isDashing || player == null) return;

                // Pohyb jen v horizontálním směru - Y pozici necháváme být
                Vector3 currentPos = player.transform.position;
                Vector3 horizontalTarget = new Vector3(dashTarget.x, currentPos.y, currentPos.z);
                
                player.transform.position = Vector3.MoveTowards(
                    currentPos, 
                    horizontalTarget, 
                    dashSpeed * Time.deltaTime
                );

                // Kontrola jestli jsme došli k cíli - kontrolujeme jen horizontální vzdálenost
                float horizontalDistance = Mathf.Abs(player.transform.position.x - dashTarget.x);
                if (horizontalDistance < 0.1f)
                {
                    // Nastavíme jen X pozici na přesnou hodnotu
                    Vector3 finalPos = player.transform.position;
                    finalPos.x = dashTarget.x;
                    player.transform.position = finalPos;
                    
                    isDashing = false;
                    Debug.Log("Dash dokončen!");
                }
            }

            void UpdateDashUI(bool isReady, float elapsedSinceLastDash)
            {
                if (dashIcon == null) return;

                // Přepni sprite podle stavu
                dashIcon.sprite = isReady ? dashReadySprite : dashCooldownSprite;

                // Vyplň kolečko podle cooldownu (0 hned po dashe, 1 když je ready)
                float fill = Mathf.Clamp01(elapsedSinceLastDash / dashCooldown);
                dashIcon.fillAmount = isReady ? 1f : fill;
            }

            // Najde nejbližší bezpečný tile na zadané Tilemapě
            Vector3 FindNearestSafeTile(Tilemap tilemap, Vector3 fromPosition)
            {
                if (tilemap == null) return Vector3.zero;

                Vector3Int playerCellPos = tilemap.WorldToCell(fromPosition);
                int searchRadiusCells = Mathf.CeilToInt(tileSearchRadius);

                Vector3 bestPosition = Vector3.zero;
                float bestDistance = Mathf.Infinity;

                // Hledáme v okolí hráče
                for (int x = -searchRadiusCells; x <= searchRadiusCells; x++)
                {
                    for (int y = -searchRadiusCells; y <= searchRadiusCells; y++)
                    {
                        Vector3Int checkPos = playerCellPos + new Vector3Int(x, y, 0);

                        if (tilemap.HasTile(checkPos))
                        {
                            Vector3Int abovePos = checkPos + new Vector3Int(0, 1, 0);
                            if (!tilemap.HasTile(abovePos))
                            {
                                Vector3 tileWorldPos = tilemap.GetCellCenterWorld(checkPos);
                                tileWorldPos.y += tilemap.cellSize.y / 2f + 1f;

                                float distance = Vector2.Distance(fromPosition, tileWorldPos);
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestPosition = tileWorldPos;
                                }
                            }
                        }
                    }
                }

                return bestPosition;
            }

            // Projde všechny relevantní Tilemapy a najde nejlepší bezpečnou pozici
            Vector3 FindNearestSafeTileAcrossTilemaps(Vector3 fromPosition, Tilemap[] tilemaps)
            {
                if (tilemaps == null || tilemaps.Length == 0) return Vector3.zero;

                Vector3 bestPosition = Vector3.zero;
                float bestDistance = Mathf.Infinity;

                foreach (var tm in tilemaps)
                {
                    if (tm == null || !tm.gameObject.activeInHierarchy) continue;
                    Vector3 candidate = FindNearestSafeTile(tm, fromPosition);
                    if (candidate != Vector3.zero)
                    {
                        float distance = Vector2.Distance(fromPosition, candidate);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestPosition = candidate;
                        }
                    }
                }

                return bestPosition;
            }

            // Získá seznam Tilemap ve scéně podle nastavení (jedna z Inspectoru, nebo automaticky nalezené)
            Tilemap[] GetRelevantTilemaps()
            {
                if (groundTilemap != null)
                {
                    return new Tilemap[] { groundTilemap };
                }

                if (!autoFindTilemaps) return System.Array.Empty<Tilemap>();

                var all = GameObject.FindObjectsOfType<Tilemap>();
                if (tilemapTags != null && tilemapTags.Length > 0)
                {
                    List<Tilemap> filtered = new List<Tilemap>();
                    foreach (var tm in all)
                    {
                        foreach (var tag in tilemapTags)
                        {
                            if (!string.IsNullOrEmpty(tag) && tm.gameObject.CompareTag(tag))
                            {
                                filtered.Add(tm);
                                break;
                            }
                        }
                    }
                    return filtered.ToArray();
                }
                return all;
            }
}

