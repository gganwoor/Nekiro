using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CounterFlash : MonoBehaviour
{
    public static CounterFlash instance;

    private Image flashImage;

    void Awake()
    {
        instance = this;

        GameObject canvasGo = new GameObject("CounterFlashCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imgGo = new GameObject("FlashImage");
        imgGo.transform.SetParent(canvasGo.transform, false);
        flashImage = imgGo.AddComponent<Image>();
        flashImage.color = new Color(1f, 0.85f, 0.1f, 0f);
        RectTransform rt = flashImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float inDuration = 0.06f;
        float outDuration = 0.35f;
        float peak = 0.45f;

        for (float t = 0f; t < inDuration; t += Time.unscaledDeltaTime)
        {
            flashImage.color = new Color(1f, 0.85f, 0.1f, Mathf.Lerp(0f, peak, t / inDuration));
            yield return null;
        }

        for (float t = 0f; t < outDuration; t += Time.unscaledDeltaTime)
        {
            flashImage.color = new Color(1f, 0.85f, 0.1f, Mathf.Lerp(peak, 0f, t / outDuration));
            yield return null;
        }

        flashImage.color = new Color(1f, 0.85f, 0.1f, 0f);
    }
}
