using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ParryEffect : MonoBehaviour
{
    public static ParryEffect instance;

    [Header("화면 플래시")]
    public Image flashImage;
    public float flashDuration = 0.12f;
    public Color flashColor = new Color(1f, 1f, 0.9f, 0.5f);

    private ParticleSystem ps;

    void Awake()
    {
        instance = this;
        ps = GetComponent<ParticleSystem>();
    }

    public void Play(Vector3 position)
    {
        transform.position = position;
        ps.Play();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        if (flashImage == null) yield break;

        flashImage.color = flashColor;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / flashDuration);
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }
}
