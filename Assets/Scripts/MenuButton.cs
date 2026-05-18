using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("연결")]
    public RectTransform icon;
    public TextMeshProUGUI buttonText;
    public Image fadePanel;
    public string targetScene;

    [Header("아이콘 위치")]
    public float leftX = 0f;
    public float centerX = -60f;
    public float rightX = 360f;

    [Header("설정")]
    public float moveSpeed = 6f;
    public float fadeDuration = 0.4f;
    public float normalFontSize = 46f;
    public float hoverFontSize = 60f;
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = new Color(0.86f, 0.78f, 0.59f, 1f);

    private float targetX;
    private bool isHovered = false;
    private bool isClicked = false;

    void Start()
    {
        targetX = leftX;
        icon.anchoredPosition = new Vector2(leftX, icon.anchoredPosition.y);
        buttonText.color = normalTextColor;
        buttonText.fontSize = normalFontSize;
    }

    void Update()
    {
        float speed = isClicked ? moveSpeed * 3f : moveSpeed;
        float currentX = icon.anchoredPosition.x;
        float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * speed);
        icon.anchoredPosition = new Vector2(newX, icon.anchoredPosition.y);

        if (!isClicked)
        {
            buttonText.color = Color.Lerp(buttonText.color, isHovered ? hoverTextColor : normalTextColor, Time.deltaTime * 3f);
            float targetSize = isHovered ? hoverFontSize : normalFontSize;
            buttonText.fontSize = Mathf.Lerp((float)buttonText.fontSize, targetSize, Time.deltaTime * 3f);
        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked) return;
        isHovered = true;
        targetX = centerX;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked) return;
        isHovered = false;
        targetX = leftX;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked) return;
        isClicked = true;
        targetX = rightX;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        float elapsed = 0f;
        Color startColor = new Color(0f, 0f, 0f, 0f);
        Color endColor = new Color(0f, 0f, 0f, 1f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
            yield return null;
        }

        if (targetScene != "")
            SceneManager.LoadScene(targetScene);
        else
            Application.Quit();
    }
}