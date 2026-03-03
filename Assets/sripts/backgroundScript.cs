using UnityEngine;

public class backgroundScript : MonoBehaviour
{
    public Transform player;
    public float parallaxSpeed = 0.2f;
    public float offset = 2f; 

    private Transform[] backgrounds;
    private float length;

    void Start()
    {
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            backgrounds[i] = transform.GetChild(i);

        if (backgrounds.Length == 0 || backgrounds[0] == null)
        {
            Debug.LogWarning("backgroundScript: žádné child pozadí nebylo nalezeno.");
            length = 0f;
            return;
        }

        var renderer = backgrounds[0].GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("backgroundScript: chybí SpriteRenderer na prvním pozadí.");
            length = 0f;
            return;
        }

        length = renderer.bounds.size.x - offset;
    }

    void Update()
    {
        // Pokud je kamera locknutá během boss fightu, nehýbej pozadím
        if (PlayerScript.cameraLocked)
            return;

        if (player == null || length == 0f || backgrounds == null || backgrounds.Length == 0)
            return;

        float parallaxX = player.position.x * parallaxSpeed;

        // Posuň každé pozadí podle hráče a jeho indexu
        for (int i = 0; i < backgrounds.Length; i++)
        {
            float bgX = Mathf.Floor((player.position.x * (1 - parallaxSpeed)) / length + i) * length + parallaxX;
            backgrounds[i].position = new Vector3(bgX, backgrounds[i].position.y, backgrounds[i].position.z);
        }
    }
}