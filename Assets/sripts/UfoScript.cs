// UFOFollowPlayer.cs
using UnityEngine;
using System.Collections;

public class UFOFollowPlayer : MonoBehaviour
{
    public GameObject player;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    public float speed = 2f;

    public GameObject laserBodyPrefab;
    public GameObject laserEndPrefab;

    private GameObject currentLaserBody;
    private GameObject currentLaserEnd;

    private bool isFiring = false;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    // Spustí střelbu na X sekund
    public void StartFiring()
    {
        if (!isFiring)
            StartCoroutine(FireForSeconds(4f));
    }

    private IEnumerator FireForSeconds(float seconds)
    {
        isFiring = true;
        float endTime = Time.time + seconds;

        while (Time.time < endTime)
        {
            Fire();          // volání logiky střelby
            yield return null; // počká do dalšího frame
        }

        // po 4 sekundách vypne laser
        if (currentLaserBody != null) currentLaserBody.SetActive(false);
        if (currentLaserEnd != null) currentLaserEnd.SetActive(false);

        isFiring = false;
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

        if (validHit.HasValue)
        {
            var hit = validHit.Value;
            float distance = hit.distance;

            Vector3 laserBodyPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.8f);
            currentLaserBody.transform.position = laserBodyPos;
            currentLaserBody.transform.localScale = new Vector3(
                currentLaserBody.transform.localScale.x,
                distance * 0.25f,
                currentLaserBody.transform.localScale.z
            );

            Vector3 laserEndPos = new Vector3(hit.point.x + 0.235f, hit.point.y - 4f, transform.position.z);
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
                20f,
                currentLaserBody.transform.localScale.z
            );
            if (currentLaserEnd != null)
                currentLaserEnd.SetActive(false);
        }

        if (currentLaserBody != null) currentLaserBody.SetActive(true);
        if (currentLaserEnd != null) currentLaserEnd.SetActive(true);
    }

    public void DestroyParentUFO()
    {
        if (transform.parent != null)
            Destroy(transform.parent.gameObject);
        else
            Debug.LogWarning("Tento objekt nemá parent! Nelze zničit parent GameObject.");
    }
}
