using System.Collections.Generic;
using UnityEngine;

public class TrajectoryDrawer : MonoBehaviour
{
    [Header("Line Settings")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.white;
    public float minPointDistance = 0.1f;

    private LineRenderer lineRenderer;
    private List<Vector3> points = new List<Vector3>();
    private bool isDrawing = false;
    private bool drawingEnabled = false;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (!drawingEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            AddPoint();
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            lineRenderer.positionCount = 0;
        }
    }

    void StartDrawing()
    {
        isDrawing = true;
        points.Clear();
        lineRenderer.positionCount = 0;
    }

    void AddPoint()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (points.Count > 0 && Vector3.Distance(mousePos, points[points.Count - 1]) < minPointDistance)
            return;

        points.Add(mousePos);
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void SetDrawingEnabled(bool enabled)
    {
        drawingEnabled = enabled;
        if (!enabled)
        {
            isDrawing = false;
            lineRenderer.positionCount = 0;
        }
    }

    public void ExecuteJudgement()
    {
        EnemyAttack enemy = FindObjectOfType<EnemyAttack>();
        EnemyStats enemyStats = FindObjectOfType<EnemyStats>();
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (enemy != null)
        {
            bool parrySuccess = JudgementSystem.CheckParry(points, enemy.attackStart, enemy.attackEnd);

            if (parrySuccess)
            {
                Debug.Log("성공");
                enemyStats.UseStamina(20f);
                playerStats.RecoverStamina(10f);
                CameraShake.instance.Shake(0.15f, 0.1f);
                HitStop.instance.Stop(0.08f);
            }
            else
            {
                Debug.Log("실패");
                playerStats.UseStamina(20f);
                CameraShake.instance.Shake(0.3f, 0.2f);
                VignetteEffect.instance.Flash();
            }
        }

        points.Clear();
    }
}