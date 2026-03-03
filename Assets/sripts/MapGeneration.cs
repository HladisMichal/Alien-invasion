using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class MapGeneration : MonoBehaviour
{
    public GameObject player;
    public GameObject zacateknahodneho;
    public GameObject invisWall;
    public GameObject zacatek;
    public List<GameObject> chunkPrefabs;
    
    // Miniboss chunky
    public List<GameObject> minibossChunkPrefabs;
    private int lastMinibossIndex = -1;

    // UFO prefab
    public GameObject ufoPrefab;
    private GameObject spawnedUfo;

    private List<GameObject> activeChunks = new List<GameObject>();
    private int lastChunkIndex = -1;
    private Coroutine ufoSpawnCoroutine;
    private float nextMilestoneBoss = 500f; // Příští skóre pro boss chunk
    private double maxScoreSoFar = 0;

    // Static reference pro přístup z jiných skriptů (miniboss)
    public static MapGeneration instance;

    public void ResetMilestonesForRestart()
    {
        nextMilestoneBoss = 500f;
        maxScoreSoFar = 0;
        lastMinibossIndex = -1;
        lastChunkIndex = -1;
        activeChunks.Clear();
    }


    public void RespawnPlatformDelayed(GameObject platform, float delay)
    {
        StartCoroutine(RespawnRoutine(platform, delay));
    }

    private IEnumerator RespawnRoutine(GameObject platform, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (platform == null)
            yield break;

        platform.SetActive(true);
        // Resetuj pozici a stav platformy
        var script = platform.GetComponent<Platformscript>();
        if (script != null)
        {
            platform.transform.position = script.startPosition;
            script.SetTargetPosition();
            // Reset dalšího stavu pokud je potřeba
        }
    }

    void Start()
    {
        // Nastav static referenci
        instance = this;

        ResetMilestonesForRestart();

        if (minibossChunkPrefabs == null || minibossChunkPrefabs.Count == 0)
        {
            Debug.LogWarning("Miniboss chunky nejsou nastavené v inspektoru (minibossChunkPrefabs je prázdné). Miniboss se nebude spawnovat.");
        }

        // Spawn první chunk na zacateknahodneho
        int index = Random.Range(0, chunkPrefabs.Count);
        lastChunkIndex = index;
        GameObject firstChunk = Instantiate(chunkPrefabs[index], Vector3.zero, Quaternion.identity);
        Transform startPoint = firstChunk.transform.Find("startPoint");
        if (startPoint != null)
        {
            Vector3 offset = firstChunk.transform.position - startPoint.position;
            firstChunk.transform.position = zacateknahodneho.transform.position + offset;
        }
        activeChunks.Add(firstChunk);

        ufoSpawnCoroutine = StartCoroutine(SpawnUfoRoutine());
    }

    void Update()
    {
        if (player == null)
        {
            var playerScript = FindObjectOfType<PlayerScript>();
            if (playerScript != null)
            {
                player = playerScript.gameObject;
            }
        }

        if (PlayerScript.skore > maxScoreSoFar)
        {
            maxScoreSoFar = PlayerScript.skore;
        }

        // Pohyb neviditelné stěny (ponechávám)
        if (player != null && invisWall != null)
        {
            float wallX = player.transform.position.x - 20f;
            if (invisWall.transform.position.x < wallX)
                invisWall.transform.position = new Vector3(wallX, invisWall.transform.position.y, invisWall.transform.position.z);
        }

        // ENDLESS GENEROVÁNÍ
        if (activeChunks.Count > 0 && player != null)
        {
            GameObject lastChunk = activeChunks[activeChunks.Count - 1];
            Transform endPoint = lastChunk.transform.Find("endPoint");
            if (endPoint != null)
            {
                float vzdalenost = Mathf.Abs(player.transform.position.x - endPoint.position.x);
                if (vzdalenost < 20f)
                {
                    // Kontrola, zda má být spawnnut miniboss chunk
                    if (maxScoreSoFar >= nextMilestoneBoss && minibossChunkPrefabs != null && minibossChunkPrefabs.Count > 0)
                    {
                        // Vyber náhodný miniboss chunk (jiný než poslední)
                        int newMinibossIndex;
                        do
                        {
                            newMinibossIndex = Random.Range(0, minibossChunkPrefabs.Count);
                        } while (newMinibossIndex == lastMinibossIndex && minibossChunkPrefabs.Count > 1);

                        lastMinibossIndex = newMinibossIndex;
                        GameObject newChunk = Instantiate(minibossChunkPrefabs[newMinibossIndex], Vector3.zero, Quaternion.identity);
                        Transform newStart = newChunk.transform.Find("startPoint");
                        if (newStart != null)
                        {
                            Vector3 offset = newChunk.transform.position - newStart.position;
                            newChunk.transform.position = endPoint.position + offset;
                        }
                        Debug.Log($"Spawned miniboss chunk: {newChunk.name} (index {newMinibossIndex}) at score {PlayerScript.skore} (max {maxScoreSoFar}).");
                        activeChunks.Add(newChunk);
                        nextMilestoneBoss += 1000f; // Dalších 1000 skóre pro příští boss
                    }
                    else
                    {
                        // Normální chunk generování
                        int newIndex;
                        do
                        {
                            newIndex = Random.Range(0, chunkPrefabs.Count);
                        } while (newIndex == lastChunkIndex && chunkPrefabs.Count > 1);

                        lastChunkIndex = newIndex;
                        GameObject newChunk = Instantiate(chunkPrefabs[newIndex], Vector3.zero, Quaternion.identity);
                        Transform newStart = newChunk.transform.Find("startPoint");
                        if (newStart != null)
                        {
                            Vector3 offset = newChunk.transform.position - newStart.position;
                            newChunk.transform.position = endPoint.position + offset;
                        }
                        activeChunks.Add(newChunk);
                    }
                }
            }
        }

                if (zacatek != null && player != null && zacateknahodneho != null)
        {
            if (player.transform.position.x > zacateknahodneho.transform.position.x + 50f)
            {
                Destroy(zacatek);
                zacatek = null; // aby se neničil opakovaně
            }
        }

        // MAZÁNÍ STARÝCH CHUNKŮ (volitelné, pro optimalizaci)
        if (activeChunks.Count > 2 && player != null)
        {
            GameObject firstChunk = activeChunks[0];
            Transform endPoint = firstChunk.transform.Find("endPoint");
            if (endPoint != null && player.transform.position.x - endPoint.position.x > 20f)
            {
                Destroy(firstChunk);
                activeChunks.RemoveAt(0);
            }
        }
    }

    private IEnumerator SpawnUfoRoutine()
    {
        while (true)
        {
            float delay = Random.Range(5f, 10f);
            yield return new WaitForSeconds(delay);

            if (ufoPrefab != null && Camera.main != null)
            {
                
                float offsetX = 0.6f;
                float offsetY = -0.6f;
                Vector3 viewportPos = new Vector3(1, 1, player.transform.position.z - Camera.main.transform.position.z);
                Vector3 spawnPos = Camera.main.ViewportToWorldPoint(viewportPos);
                spawnPos.x += offsetX;
                spawnPos.y += offsetY;
                Instantiate(ufoPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    // Vypne spawning normálních UFO (pro miniboss fight)
    public void StopSpawningUFO()
    {
        if (ufoSpawnCoroutine != null)
        {
            StopCoroutine(ufoSpawnCoroutine);
            ufoSpawnCoroutine = null;
        }
    }

    // Zapne spawning normálních UFO (po miniboss fightu)
    public void StartSpawningUFO()
    {
        if (ufoSpawnCoroutine == null)
        {
            ufoSpawnCoroutine = StartCoroutine(SpawnUfoRoutine());
        }
    }
}