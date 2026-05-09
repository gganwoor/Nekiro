using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("예고선 설정")]
    public float warningDuration = 2f;
    public float lineWidth = 0.05f;
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("공격 설정")]
    public Vector3 attackStart = new Vector3(-3f, 0f, 0f);
    public Vector3 attackEnd = new Vector3(3f, 0f, 0f);

    private LineRenderer warningLine;
    public bool isWarningActive = false;

    void Start()
    {
        warningLine = gameObject.AddComponent<LineRenderer>();
        warningLine.startWidth = lineWidth;
        warningLine.endWidth = lineWidth;
        warningLine.startColor = warningColor;
        warningLine.endColor = warningColor;
        warningLine.positionCount = 2;
    }

    public void StartAttack()
    {
        StartCoroutine(ShowWarning());
    }

    IEnumerator ShowWarning()
    {
        isWarningActive = true;
        warningLine.SetPosition(0, attackStart);
        warningLine.SetPosition(1, attackEnd);
        warningLine.enabled = true;

        float elapsed = 0f;
        while(elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(elapsed * 3f, 1f);
            warningLine.startColor = new Color(1f, 0.3f, 0.3f, alpha);
            warningLine.endColor = new Color(1f, 0.3f, 0.3f, alpha);
            yield return null;
        }

        isWarningActive = false;
        warningLine.enabled = false;
    }
}