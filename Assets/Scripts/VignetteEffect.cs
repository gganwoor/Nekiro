using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VignetteEffect : MonoBehaviour
{
    public static VignetteEffect instance;

    public Image vignetteImage;
    public float maxAlpha = 0.5f;
    public float fadeSpeed = 3f;

    private Coroutine currentRoutine;

    void Awake()
    {
        instance = this;
    }

    public void Flash()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color color = new Color(1f, 0f, 0f, maxAlpha);
        while (color.a > 0f)
        {
            vignetteImage.color = color;
            yield return null;
            color.a = Mathf.Max(color.a - Time.deltaTime * fadeSpeed, 0f);
        }
        vignetteImage.color = new Color(1f, 0f, 0f, 0f);
    }
}