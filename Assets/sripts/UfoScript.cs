// UFOFollowPlayer.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UFOFollowPlayer : MonoBehaviour
{
    public GameObject player;

    public GameObject laserBodyPrefab;
    public GameObject laserEndPrefab;

    // Kdyz je true, UFO strili laser nepretrzite (pro arenu minibosse)
    public bool continuousFire = false;

    public Animator animator;
    public Collider2D stopFireHitbox;

    private GameObject currentLaserBody;
    private GameObject currentLaserEnd;

    private bool isFiring = false;
    private bool barrierActive = false;
    private Coroutine fireCoroutine; // Pro zastavení coroutine
    [Range(0f, 1f)] public float ufoSpawnLoopVolume = 1f;
    [Range(0f, 1f)] public float ufoFireLoopVolume = 1f;
    private bool spawnLoopRequested = false;
    private bool fireLoopRequested = false;


    void Update()
    {
        CheckStopFireHitbox();

        // Pokud je zapnuta kontinuální palba, strilej kazdy frame
        if (continuousFire)
        {
            StartUfoFireLoop();
            Fire();
            return;
        }

        if (!isFiring)
        {
            StopUfoFireLoop();
        }
    }

    void OnEnable()
    {
        StartUfoSpawnLoop();
    }

    void OnDisable()
    {
        StopUfoFireLoop();
        StopUfoSpawnLoop();
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    // Spustí střelbu na X sekund
    public void StartFiring()
    {
        // Pokud je nastavena kontinuální palba, nespoustej casovy burst
        if (continuousFire)
            return;

        if (!isFiring && fireCoroutine == null)
            fireCoroutine = StartCoroutine(FireForSeconds(4f));
    }

    private IEnumerator FireForSeconds(float seconds)
    {
        isFiring = true;
        StartUfoFireLoop();
        float endTime = Time.time + seconds;

        while (Time.time < endTime)
        {
            Fire();          // volání logiky střelby
            yield return null; // počká do dalšího frame
        }

        // po 4 sekundách vypne laser
        HideLaser();

        isFiring = false;
        fireCoroutine = null;
    }

    private void Fire()
    {
        if (currentLaserBody == null)
            currentLaserBody = Instantiate(laserBodyPrefab, transform.position, Quaternion.identity, transform);

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 100f);
        RaycastHit2D? validHit = null;

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Platform")))
            {
                validHit = hit;
                break;
            }
        }

        bool hasValidHit = validHit.HasValue;

        if (hasValidHit)
        {
            var hit = validHit.Value;
            float distance = hit.distance;

            Vector3 laserBodyPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.8f);
            currentLaserBody.transform.position = laserBodyPos;
            currentLaserBody.transform.localScale = new Vector3(
                currentLaserBody.transform.localScale.x,
                distance * 2.5f,
                currentLaserBody.transform.localScale.z
            );

            // Laser end - spawne se na zemi
            Vector3 laserEndPos = new Vector3(hit.point.x + 0.025f, hit.point.y - 0.35f, transform.position.z);
            if (currentLaserEnd == null)
                currentLaserEnd = Instantiate(laserEndPrefab, laserEndPos, Quaternion.identity, transform);
            else
                currentLaserEnd.transform.position = laserEndPos;

            Vector3 endScale = currentLaserEnd.transform.localScale;
            endScale.x = 1.68f;
            endScale.y = 1.68f;
            currentLaserEnd.transform.localScale = endScale;
        }
        else
        {
            Vector3 laserBodyPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + 1f);
            currentLaserBody.transform.position = laserBodyPos;
            currentLaserBody.transform.localScale = new Vector3(
                currentLaserBody.transform.localScale.x,
                30f,
                currentLaserBody.transform.localScale.z
            );
            if (currentLaserEnd != null)
                currentLaserEnd.SetActive(false);
        }

        if (currentLaserBody != null) currentLaserBody.SetActive(true);
        if (currentLaserEnd != null) currentLaserEnd.SetActive(hasValidHit);
    }

    private void CheckStopFireHitbox()
    {
        if (stopFireHitbox == null)
            return;

        if (!continuousFire && !isFiring)
            return;

        List<Collider2D> hitResults = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        Physics2D.OverlapCollider(stopFireHitbox, filter, hitResults);
        foreach (Collider2D hit in hitResults)
        {
            if (hit != null && hit.GetComponent<Bulletscript>() != null)
            {
                StopFiringNow();
                return;
            }
        }
    }

    private void StopFiringNow()
    {
        continuousFire = false;

        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }

        isFiring = false;
        HideLaser();
    }

    public void SetContinuousFire(bool enabled)
    {
        continuousFire = enabled;
        if (!continuousFire)
        {
            // Zastavit coroutine střelby
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
                fireCoroutine = null;
            }
            isFiring = false;
            // Pri vypnuti kontinuální palby okamzite schovej laser
            HideLaser();
        }
    }

    private void HideLaser()
    {
        if (currentLaserBody != null) currentLaserBody.SetActive(false);
        if (currentLaserEnd != null) currentLaserEnd.SetActive(false);
        StopUfoFireLoop();
    }

    private void StartUfoSpawnLoop()
    {
        if (spawnLoopRequested) return;
        if (SFXManagerScript.Instance == null) return;

        SFXManagerScript.Instance.StartLoopSFX(SFXManagerScript.LoopSfxId.UfoSpawn, ufoSpawnLoopVolume);
        spawnLoopRequested = true;
    }

    private void StopUfoSpawnLoop()
    {
        if (!spawnLoopRequested) return;
        if (SFXManagerScript.Instance != null)
        {
            SFXManagerScript.Instance.StopLoopSFX(SFXManagerScript.LoopSfxId.UfoSpawn);
        }
        spawnLoopRequested = false;
    }

    private void StartUfoFireLoop()
    {
        if (fireLoopRequested) return;
        if (SFXManagerScript.Instance == null) return;

        SFXManagerScript.Instance.StartLoopSFX(SFXManagerScript.LoopSfxId.UfoFire, ufoFireLoopVolume);
        fireLoopRequested = true;
    }

    private void StopUfoFireLoop()
    {
        if (!fireLoopRequested) return;
        if (SFXManagerScript.Instance != null)
        {
            SFXManagerScript.Instance.StopLoopSFX(SFXManagerScript.LoopSfxId.UfoFire);
        }
        fireLoopRequested = false;
    }

    // === METODY PRO ANIMATOR A BARIERU ===
    
    // Spusti animaci priletu a aktivaci bariery
    public void ActivateBarrier()
    {
        if (barrierActive) return;
        if (animator != null)
        {
            animator.SetTrigger("Prilet");
        }
    }

    // Spusti animaci odletu a deaktivaci bariery
    public void DeactivateBarrier()
    {
        if (animator != null)
        {
            animator.SetTrigger("Odlet");
        }
    }

    // Animation Event - vola se na konci animace Prilet
    public void OnArrived()
    {
        barrierActive = true;
        SetContinuousFire(true);
    }

    // Animation Event - vola se na zacatku animace Odlet
    public void OnDepart()
    {
        SetContinuousFire(false);
        barrierActive = false;
    }

    // Animation Event - vola se na KONCI animace Odlet
    public void OnDepartFinished()
    {
        gameObject.SetActive(false);
    }

    public void DestroyParentUFO()
    {
        if (transform.parent != null)
            Destroy(transform.parent.gameObject);
        else
            Debug.LogWarning("Tento objekt nemá parent! Nelze zničit parent GameObject.");
    }
}
