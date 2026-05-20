using System.Collections;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    public static EnemyHitFlash instance;

    public SpriteRenderer spriteRenderer;
    public float flashDuration = 0.08f;

    private Color originalColor;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSecondsRealtime(flashDuration);
        spriteRenderer.color = originalColor;
    }
}
