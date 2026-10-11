using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapFadeOnTouch : MonoBehaviour
{
    private Tilemap tilemap;
    private float targetAlpha = 1f;
    public float fadeSpeed = 5f; // Speed of the fade transition

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("TilemapFadeOnTouch requires a Tilemap component on the same GameObject!");
        }
    }

    void Update()
    {
        if (tilemap == null) return;

        // Smoothly interpolate the tilemap's global color alpha toward the target alpha
        Color color = tilemap.color;
        color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
        tilemap.color = color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetAlpha = 0.2f; // Fade out
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetAlpha = 1.0f; // Return to fully opaque
        }
    }
}