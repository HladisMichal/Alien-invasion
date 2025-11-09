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

    // UFO prefab
    public GameObject ufoPrefab;
    private GameObject spawnedUfo;

    private List<GameObject> activeChunks = new List<GameObject>();
    private int lastChunkIndex = -1;


    public void RespawnPlatformDelayed(GameObject platform, float delay)
    {
        StartCoroutine(RespawnRoutine(platform, delay));
    }

    private IEnumerator RespawnRoutine(GameObject platform, float delay)
    {
        yield return new WaitForSeconds(delay);
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

        StartCoroutine(SpawnUfoRoutine());
    }

    void Update()
    {
        // Pohyb neviditelné stěny (ponechávám)
        if (player != null && invisWall != null)
        {
            float wallX = player.transform.position.x - 150f;
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
                if (vzdalenost < 100f)
                {
                    // Najdi jiný chunk než poslední
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

                if (zacatek != null && player != null && zacateknahodneho != null)
        {
            if (player.transform.position.x > zacateknahodneho.transform.position.x + 200f)
            {
                Destroy(zacatek);
                zacatek = null; // aby se neničil opakovaně
            }
        }

        // MAZÁNÍ STARÝCH CHUNKŮ (volitelné, pro optimalizaci)
        if (activeChunks.Count > 2 && player != null)
        {
            GameObject firstChunk = activeChunks[0];
            Transform startPoint = firstChunk.transform.Find("startPoint");
            if (startPoint != null && player.transform.position.x - startPoint.position.x > 200f)
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
            float delay = Random.Range(20f, 55f);
            yield return new WaitForSeconds(delay);

            if (ufoPrefab != null && Camera.main != null)
            {
                
                float offsetX = 3f;
                float offsetY = -3f;
                Vector3 viewportPos = new Vector3(1, 1, player.transform.position.z - Camera.main.transform.position.z);
                Vector3 spawnPos = Camera.main.ViewportToWorldPoint(viewportPos);
                spawnPos.x += offsetX;
                spawnPos.y += offsetY;
                Instantiate(ufoPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
}