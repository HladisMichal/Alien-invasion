using UnityEngine;

public class Rusherscript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Transform player;
    private bool isVisible = false;
    private Rigidbody2D rb;
    private float scale;
    public float damageCooldown = 1f;
    private float lastDamageTime = 0f;
    private float lastJumpTime = -1f;
    public float jumpCooldown = 0.9f;
    private bool isGrounded = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        scale = transform.localScale.x;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isVisible && player != null)
        {
            ChasePlayer();
        }
        else if (!isVisible)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void ChasePlayer()
    {
        float directionX = player.position.x - transform.position.x;
        
        if (directionX < 0)
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
            transform.localScale = new Vector3(-scale, scale, 1);
            CheckJump(-1);
        }
        else if (directionX > 0)
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
            transform.localScale = new Vector3(scale, scale, 1);
            CheckJump(1);
        }
    }

    void CheckJump(int direction)
    {
        bool canJump = Time.time - lastJumpTime >= jumpCooldown;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        int rayMask = Physics2D.DefaultRaycastLayers & ~(1 << gameObject.layer);

        Vector2 checkPos = new Vector2(col.bounds.center.x, col.bounds.center.y);
        Vector2 obstacleCheckPos = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.15f);
        Vector2 gapCheckPos = new Vector2(col.bounds.center.x + direction * (col.bounds.extents.x + 0.5f), col.bounds.min.y + 0.9f);

        float obstacleRayDistance = 1.3f;
        float groundRayDistance = col.bounds.extents.y + 0.35f;
        float gapRayDistance = col.bounds.extents.y + 1.25f;

        RaycastHit2D hitObstacle = Physics2D.Raycast(obstacleCheckPos, Vector2.right * direction, obstacleRayDistance, rayMask);
        RaycastHit2D hitGround = Physics2D.Raycast(checkPos, Vector2.down, groundRayDistance, rayMask);
        RaycastHit2D hitGap = Physics2D.Raycast(gapCheckPos, Vector2.down, gapRayDistance, rayMask);

        Debug.DrawLine(obstacleCheckPos, obstacleCheckPos + Vector2.right * direction * obstacleRayDistance, Color.red);
        Debug.DrawLine(checkPos, checkPos + Vector2.down * groundRayDistance, Color.green);
        Debug.DrawLine(gapCheckPos, gapCheckPos + Vector2.down * gapRayDistance, Color.yellow);

        if (canJump)
        {
            bool onGround = hitGround.collider != null && (hitGround.collider.CompareTag("Ground") || hitGround.collider.CompareTag("Platform"));
            bool obstacleAhead = hitObstacle.collider != null && (hitObstacle.collider.CompareTag("Ground") || hitObstacle.collider.CompareTag("Platform"));
            bool groundAfterGap = hitGap.collider != null && (hitGap.collider.CompareTag("Ground") || hitGap.collider.CompareTag("Platform"));

            if (onGround && obstacleAhead)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                lastJumpTime = Time.time;
                Debug.Log("JUMP! - Překážka");
            }
            else if (!onGround && groundAfterGap)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                lastJumpTime = Time.time;
                Debug.Log("JUMP! - Mezera");
            }
        }
    }

    void OnBecameVisible()
    {
        isVisible = true;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
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
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerScript playerScript = collision.gameObject.GetComponent<PlayerScript>();
                if (playerScript != null)
                {
                    playerScript.OdeberZivoty();
                    Debug.Log("Hráč dostal hit od Rushera! Životy: " + playerScript.zivoty);
                    lastDamageTime = Time.time;
                }
            }
        }
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Rusher byl zničen!");
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            PlayerScript playerScript = playerObject.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                PlayerScript.skore += 150;
            }
        }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        Destroy(gameObject);
    }
}
