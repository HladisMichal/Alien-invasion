using UnityEngine;

public class Platformscript : MonoBehaviour
{
    public int smer = 1; // 1 = nahoru/dolu, 2 = doleva/doprava
    public float speed = 5f;
    public float moveDistance = 3f; // vzdálenost pohybu od výchozí pozice
    public float waitTime = 1f;     // doba čekání na konci pohybu
    public int direction = 1; // 1 nebo -1

    public bool breakable = false;      // nastav v inspektoru, zda je platforma zničitelná
    public float breakDelay = 3f;
    public float respawnDelay = 5f;     // čas po kterém se platforma znovu objeví aktivaci
    private bool breaking = false;      // interní příznak, zda už se ničí
    private float breakTimer = 0f;
    private MapGeneration mapGen;

    public Vector3 startPosition;
    private Vector3 targetPosition;
    
    private float waitTimer = 0f;
    private bool waiting = false;

    private bool shouldMove = false; // pro režim 3, kdy se platforma pohybuje jen jednou směrem k cíli
    private bool isReturning = false; // pro režim 3 a 4, kdy se vrací na původní místo

    void Start()
    {
        startPosition = transform.position;
        SetTargetPosition();
        mapGen = FindFirstObjectByType<MapGeneration>();
    }

    void Update()
    {

        // Pokud je platforma v režimu ničení
        if (breaking && breakable)
        {
            breakTimer -= Time.deltaTime;
            if (breakTimer <= 0f)
            {
                foreach (Transform child in transform)
                {
                    if (child.CompareTag("Player"))
                        child.SetParent(null, true);
                }
                if (mapGen != null)
                    mapGen.RespawnPlatformDelayed(gameObject, respawnDelay);
                gameObject.SetActive(false);
                breaking = false;
            }
            return;
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                if (smer == 1 || smer == 2)
                {
                    direction *= -1;
                    SetTargetPosition();
                }
                else if (smer == 3 || smer == 4)
                {
                    // Pro režim 3 a 4 se vrací na původní místo
                    isReturning = true;
                }
            }
            return;
        
        }

        if (smer == 3)
        {
            // Režim 3: Horizontální pohyb (doleva/doprava) na dotek
            if (!shouldMove && !isReturning)
                return; // čeká na dotyk hráče

            if (!isReturning)
            {
                // Pohyb směrem k cíli
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    shouldMove = false;
                    waiting = true;
                    waitTimer = waitTime; // čeká před vrácením
                }
            }
            else
            {
                // Návrat na původní místo
                transform.position = Vector3.MoveTowards(transform.position, startPosition, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, startPosition) < 0.01f)
                {
                    isReturning = false;
                    shouldMove = false;
                }
            }
            return;
        }

        if (smer == 4)
        {
            // Režim 4: Vertikální pohyb (nahoru/dolů) na dotek
            if (!shouldMove && !isReturning)
                return; // čeká na dotyk hráče

            if (!isReturning)
            {
                // Pohyb směrem k cíli
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    shouldMove = false;
                    waiting = true;
                    waitTimer = waitTime; // čeká před vrácením
                }
            }
            else
            {
                // Návrat na původní místo
                transform.position = Vector3.MoveTowards(transform.position, startPosition, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, startPosition) < 0.01f)
                {
                    isReturning = false;
                    shouldMove = false;
                }
            }
            return;
        }

        if (smer != 1 && smer != 2)
            return;

        // Pohyb směrem k cílové pozici
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Pokud jsme dorazili na cíl, začni čekat
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            waiting = true;
            waitTimer = waitTime;
        }
    }


    private System.Collections.IEnumerator RespawnPlatform()
    {
        yield return new WaitForSeconds(respawnDelay);
        gameObject.SetActive(true);
        transform.position = startPosition;
        SetTargetPosition();
        waiting = false;
        breakTimer = 0f;
        Debug.Log("Platforma byla znovu aktivována.");
    }

    public void SetTargetPosition()
    {
        if (smer == 1)
        {
            targetPosition = startPosition + new Vector3(0, moveDistance * direction, 0);
        }
        else if (smer == 2)
        {
            targetPosition = startPosition + new Vector3(moveDistance * direction, 0, 0);
        }
        else if (smer == 3)
        {
            // Režim 3: Horizontální pohyb
            targetPosition = startPosition + new Vector3(moveDistance * direction, 0, 0);
        }
        else if (smer == 4)
        {
            // Režim 4: Vertikální pohyb
            targetPosition = startPosition + new Vector3(0, moveDistance * direction, 0);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (breakable && !breaking && collision.gameObject.CompareTag("Player"))
        {
            breaking = true;
            breakTimer = breakDelay;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(this.transform, true); // zachová pozici a scale
        }

        if ((smer == 3 || smer == 4) && !shouldMove && !isReturning)
        {
            shouldMove = true;
            SetTargetPosition();
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null, true);
        }
    }
}
