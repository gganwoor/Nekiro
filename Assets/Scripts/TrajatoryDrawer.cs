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

    private PlayerStats playerStats;
    private EnemyAttack enemyAttack;
    private EnemyStats enemyStats;

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

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        enemyAttack = FindObjectOfType<EnemyAttack>();
        enemyStats = FindObjectOfType<EnemyStats>();
    }

    [HideInInspector] public bool isReadyToJudge = false;

    void Update()
    {
        if (!drawingEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (playerStats != null && playerStats.currentStamina <= 0)
            {
                CameraShake.instance.Shake(0.15f, 0.05f);
                VignetteEffect.instance.Flash();
                return;
            }
            StartDrawing();
        }

        if (Input.GetMouseButton(0) && isDrawing)
            AddPoint();

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            lineRenderer.positionCount = 0;
            isReadyToJudge = true;
            BattleManager.instance?.EndHoldBreath();
        }
    }

    public void ResetJudgement()
    {
        isReadyToJudge = false;
        points.Clear();
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
        if (enemyAttack == null) return;

        bool parrySuccess = JudgementSystem.CheckParry(points, enemyAttack.attackStart, enemyAttack.attackEnd);

        if (parrySuccess)
        {
            PlayerAttack.instance?.PlayParry();
            if (enemyStats.currentStamina > 0)
                enemyStats.UseStamina(20f);
            else
                enemyStats.TakeDamage(20f);
            playerStats.RecoverStamina(10f);
            EnemyHitFlash.instance?.Flash();
            CameraShake.instance.Shake(0.15f, 0.1f);
            HitStop.instance.Stop(0.08f);
            Vector3 effectPos = Vector3.Lerp(enemyAttack.attackStart, enemyAttack.attackEnd, 0.25f);
            ParryEffect.instance?.Play(effectPos);

            if (TutorialManager.instance != null &&
                TutorialManager.instance.currentStep == TutorialManager.TutorialStep.ParryPractice)
                TutorialManager.instance.CompleteStep();
        }
        else if (PlayerDash.instance != null && PlayerDash.instance.IsInvincible)
        {
            // 대시 회피 성공 — 피해 없음
        }
        else
        {
            PlayerAttack.instance?.PlayHit();
            playerStats.UseStamina(20f);
            playerStats.TakeDamage(10f);
            CameraShake.instance.Shake(0.3f, 0.2f);
            VignetteEffect.instance.Flash();
        }

        points.Clear();
    }
}
