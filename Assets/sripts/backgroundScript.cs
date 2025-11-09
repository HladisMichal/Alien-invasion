using UnityEngine;

public class backgroundScript : MonoBehaviour
{
    public Transform player;
    public float parallaxSpeed = 0.5f;
    public float offset = 2f; 

    private Transform[] backgrounds;
    private float length;

    void Start()
    {
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            backgrounds[i] = transform.GetChild(i);

        length = backgrounds[0].GetComponent<SpriteRenderer>().bounds.size.x - offset;
    }

    void Update()
    {
        float parallaxX = player.position.x * parallaxSpeed;

        // Posuň každé pozadí podle hráče a jeho indexu
        for (int i = 0; i < backgrounds.Length; i++)
        {
            float bgX = Mathf.Floor((player.position.x * (1 - parallaxSpeed)) / length + i) * length + parallaxX;
            backgrounds[i].position = new Vector3(bgX, backgrounds[i].position.y, backgrounds[i].position.z);
        }
    }
}