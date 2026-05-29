using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [System.Serializable]
    public struct AttackPattern
    {
        public string name;
        public Vector2 startOffset;
        public Vector2 endOffset;
        public bool aimAtPlayer;
    }

    [Header("예고선 설정")]
    public float warningDuration = 2f;
    public float briefWarningDuration = 0.4f;
    public float lineWidth = 0.05f;
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("슬래시 이펙트")]
    public float slashDuration = 0.2f;
    public float slashWidth = 0.12f;
    public Color slashColor = Color.white;

    [Header("공격 타이밍")]
    public float initialDelay = 3f;
    public float attackInterval = 5f;

    [Header("콤보")]
    public int minComboCount = 1;
    public int maxComboCount = 3;
    public float comboPauseDuration = 0.8f;

    [Header("AI 패턴 전환 거리")]
    public float closeRangeThreshold = 5f;

    [Header("히트박스")]
    public float hitRadius = 0.8f;
    public float hitStartTime = 0.25f;
    public float hitEndTime   = 0.75f;

    [Header("애니메이션 속도")]
    public float attackAnimSpeed = 1.8f;

    [Header("고스트 미리보기")]
    public float ghostAnimSpeed = 0.6f;

    [Header("에스컬레이션 (HP 낮을수록 강해짐)")]
    public bool escalationEnabled = true;
    public float minAttackInterval = 2.0f;
    public float maxAttackAnimSpeed = 2.8f;

    [Header("공격 패턴")]
    public AttackPattern[] patterns = new AttackPattern[]
    {
        new AttackPattern { name = "수평 베기",   startOffset = new Vector2(1f,  0.5f), endOffset = new Vector2(-5f,  0.5f) },
        new AttackPattern { name = "대각선 ↘",   startOffset = new Vector2(0f,  2f),   endOffset = new Vector2(-4f, -1f)   },
        new AttackPattern { name = "대각선 ↗",   startOffset = new Vector2(0f, -1f),   endOffset = new Vector2(-4f,  2f)   },
        new AttackPattern { name = "찌르기",      startOffset = new Vector2(0f,  0f),   endOffset = Vector2.zero, aimAtPlayer = true },
    };

    [HideInInspector] public Vector3 attackStart;
    [HideInInspector] public Vector3 attackEnd;

    private bool playerWasInPath = false;
    public bool PlayerWasInPath => playerWasInPath;

    private LineRenderer warningLine;
    private LineRenderer slashLine;
    private Animator animator;
    private Transform playerTransform;
    private TrajectoryDrawer trajectoryDrawer;
    private EnemyStats enemyStats;
    private int lastPatternIndex = -1;
    private float attackTimer = 0f;
    private bool attackCycleRunning = false;
    private bool isExecutingAttack = false;
    private bool battleWasRunning = false;
    private bool playerInRange = false;
    private GameObject ghostVisual;
    private Animator ghostAnimator;
    private SpriteRenderer[] ghostSpriteRenderers;
    private bool ghostSpawned = false;
    private string ghostClipName = "";
    private int ghostCycleCount = 0;
    private float ghostPrevNT = 0f;

    public bool IsAttackCycleRunning => attackCycleRunning;
    public bool IsExecutingAttack => isExecutingAttack;

    public void InterruptAttack()
    {
        StopAllCoroutines();
        attackCycleRunning = false;
        isExecutingAttack = false;
        if (warningLine != null) warningLine.enabled = false;
        if (slashLine != null) slashLine.enabled = false;
        HideGhostEchoes();
    }

    void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;
        trajectoryDrawer = FindFirstObjectByType<TrajectoryDrawer>();
        animator = GetComponentInChildren<Animator>(true);
        enemyStats = GetComponent<EnemyStats>();

        warningLine = gameObject.AddComponent<LineRenderer>();
        warningLine.startWidth = lineWidth;
        warningLine.endWidth = lineWidth;
        warningLine.startColor = warningColor;
        warningLine.endColor = warningColor;
        warningLine.positionCount = 2;
        warningLine.material = new Material(Shader.Find("Sprites/Default"));
        warningLine.enabled = false;

        GameObject slashObj = new GameObject("SlashLine");
        slashObj.transform.SetParent(transform);
        slashLine = slashObj.AddComponent<LineRenderer>();
        slashLine.startWidth = slashWidth;
        slashLine.endWidth = slashWidth;
        slashLine.positionCount = 2;
        slashLine.material = new Material(Shader.Find("Sprites/Default"));
        slashLine.sortingOrder = 5;
        slashLine.enabled = false;
    }

    void Update()
    {
        if (BattleManager.instance == null) return;

        bool isRunning = BattleManager.instance.IsBattleRunning;

        if (isRunning && !battleWasRunning)
        {
            attackTimer = -initialDelay;
            attackCycleRunning = false;
        }
        battleWasRunning = isRunning;

        if (!isRunning)
        {
            warningLine.enabled = false;
            return;
        }

        if (attackCycleRunning) return;
        if (enemyStats != null && enemyStats.IsGroggy) return;

        float currentInterval = escalationEnabled
            ? Mathf.Lerp(minAttackInterval, attackInterval, GetEscalationRatio())
            : attackInterval;

        bool nowInRange = IsPlayerInRange();

        if (!nowInRange)
        {
            attackTimer = Mathf.Min(attackTimer, 0f);
            playerInRange = false;
            return;
        }

        if (!playerInRange)
            attackTimer = Mathf.Max(attackTimer, currentInterval - 0.4f);

        playerInRange = true;
        attackTimer += Time.unscaledDeltaTime;
        if (attackTimer >= currentInterval)
        {
            attackTimer = 0f;
            StartCoroutine(AttackCycle());
        }
    }

    bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        Vector3 referencePos = EnemyAI.instance != null && EnemyAI.instance.visualTransform != null
            ? EnemyAI.instance.visualTransform.position
            : transform.position;
        return Mathf.Abs(referencePos.x - playerTransform.position.x) <= closeRangeThreshold;
    }

    float GetEscalationRatio()
    {
        if (enemyStats == null || enemyStats.maxHP <= 0f) return 1f;
        return Mathf.Clamp01(enemyStats.currentHP / enemyStats.maxHP);
    }

    // 점 p와 선분 a-b 사이 최단 거리
    float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.0001f) return Vector3.Distance(p, a);
        float proj = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return Vector3.Distance(p, a + proj * ab);
    }

    IEnumerator AttackCycle()
    {
        attackCycleRunning = true;

        int comboCount = Random.Range(minComboCount, maxComboCount + 1);

        for (int i = 0; i < comboCount; i++)
        {
            SelectPattern();

            yield return StartCoroutine(ShowWarning());
            yield return StartCoroutine(ExecuteAttack());

            trajectoryDrawer?.ExecuteJudgement();
            trajectoryDrawer?.ResetJudgement();

            if (i < comboCount - 1)
                yield return new WaitForSecondsRealtime(comboPauseDuration);
        }

        yield return new WaitForSecondsRealtime(0.8f);

        if (BattleManager.instance != null)
            BattleManager.instance.battleCycleComplete = true;

        attackCycleRunning = false;
    }

    void SelectPattern()
    {
        if (patterns.Length == 0) return;

        int[] candidates;
        bool useDistanceAI = TutorialManager.instance == null ||
                             TutorialManager.instance.currentStep == TutorialManager.TutorialStep.RealBattle;

        Vector3 visualPos = EnemyAI.instance != null && EnemyAI.instance.visualTransform != null
            ? EnemyAI.instance.visualTransform.position
            : transform.position;

        if (useDistanceAI && playerTransform != null)
        {
            float dist = Mathf.Abs(visualPos.x - playerTransform.position.x);
            candidates = dist < closeRangeThreshold ? new int[] { 0, 3 } : new int[] { 1, 2 };
        }
        else
        {
            candidates = new int[] { 0, 1, 2, 3 };
        }

        int index;
        do { index = candidates[Random.Range(0, candidates.Length)]; }
        while (index == lastPatternIndex && candidates.Length > 1);
        lastPatternIndex = index;

        AttackPattern p = patterns[index];
        Vector3 origin = visualPos;

        attackStart = origin + (Vector3)(Vector2)p.startOffset;

        if (p.aimAtPlayer && playerTransform != null)
        {
            Vector3 dir = (playerTransform.position - origin).normalized;
            attackEnd = playerTransform.position + dir * 1.5f;
        }
        else
        {
            attackEnd = origin + (Vector3)(Vector2)p.endOffset;
        }

        attackStart.z = 0f;
        attackEnd.z = 0f;

        warningLine.SetPosition(0, attackStart);
        warningLine.SetPosition(1, attackEnd);
    }

    IEnumerator ShowWarning()
    {
        warningLine.enabled = false;

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (trajectoryDrawer != null && trajectoryDrawer.isReadyToJudge)
                break;

            bool holding = BattleManager.instance != null && BattleManager.instance.IsHoldingBreath;
            if (holding)
                UpdateGhostEchoes(elapsed);
            else
                HideGhostEchoes();

            if (ghostSpawned && ghostCycleCount >= 2)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HideGhostEchoes();

        if (trajectoryDrawer == null || !trajectoryDrawer.isReadyToJudge)
        {
            warningLine.startColor = warningColor;
            warningLine.endColor = warningColor;
            warningLine.enabled = true;
            yield return new WaitForSecondsRealtime(briefWarningDuration);
        }

        warningLine.enabled = false;
    }

    void UpdateGhostEchoes(float elapsed)
    {
        if (animator == null) return;

        if (!ghostSpawned)
        {
            ghostVisual = Instantiate(animator.gameObject,
                                      animator.transform.position,
                                      animator.transform.rotation);
            ghostVisual.transform.localScale = animator.transform.lossyScale;

            ghostAnimator = ghostVisual.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                ghostClipName = (lastPatternIndex == 1 || lastPatternIndex == 2) ? "Attack2" : "Attack1";
                ghostCycleCount = 0;
                ghostPrevNT = 0f;
                ghostAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                ghostAnimator.speed = ghostAnimSpeed;
                ghostAnimator.Play(ghostClipName, 0, 0f);
            }

            ghostSpriteRenderers = ghostVisual.GetComponentsInChildren<SpriteRenderer>(true);
            ghostSpawned = true;
        }

        if (ghostAnimator != null && ghostAnimator.speed > 0f)
        {
            float nt = ghostAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (nt >= 1f && ghostPrevNT < 1f)
            {
                ghostCycleCount++;
                if (ghostCycleCount >= 2)
                    ghostAnimator.speed = 0f;
                else
                {
                    ghostPrevNT = 1f;
                    ghostAnimator.Play(ghostClipName, 0, 0f);
                }
            }
            else
            {
                ghostPrevNT = nt;
            }
        }

        if (ghostVisual != null)
            ghostVisual.transform.position = animator.transform.position;

        float pulse = 0.7f + 0.3f * Mathf.PingPong(elapsed * 2.5f, 1f);
        if (ghostSpriteRenderers != null)
            foreach (var sr in ghostSpriteRenderers)
                if (sr != null) sr.color = new Color(0.55f, 0.75f, 1f, 0.32f * pulse);
    }

    void HideGhostEchoes()
    {
        ghostSpawned = false;
        if (ghostVisual != null)
        {
            Destroy(ghostVisual);
            ghostVisual = null;
            ghostAnimator = null;
            ghostSpriteRenderers = null;
        }
    }

    void OnDestroy()
    {
        HideGhostEchoes();
    }

    IEnumerator ExecuteAttack()
    {
        isExecutingAttack = true;
        playerWasInPath = false;
        warningLine.enabled = false;

        if (BattleManager.instance != null && BattleManager.instance.IsHoldingBreath)
            BattleManager.instance.EndHoldBreath();

        // 실행 시점 적 위치로 재계산 (ShowWarning 중 이동 반영)
        Vector3 visualPos = EnemyAI.instance?.visualTransform != null
            ? EnemyAI.instance.visualTransform.position
            : transform.position;
        AttackPattern p = patterns[lastPatternIndex];
        attackStart = visualPos + (Vector3)(Vector2)p.startOffset;
        if (p.aimAtPlayer && playerTransform != null)
        {
            Vector3 dir = (playerTransform.position - visualPos).normalized;
            attackEnd = playerTransform.position + dir * 1.5f;
        }
        else
        {
            attackEnd = visualPos + (Vector3)(Vector2)p.endOffset;
        }
        attackStart.z = 0f;
        attackEnd.z = 0f;

        string attackClip = (lastPatternIndex == 1 || lastPatternIndex == 2) ? "Attack2" : "Attack1";
        float currentAnimSpeed = escalationEnabled
            ? Mathf.Lerp(maxAttackAnimSpeed, attackAnimSpeed, GetEscalationRatio())
            : attackAnimSpeed;
        if (animator != null) animator.speed = currentAnimSpeed;
        animator?.Play(attackClip, 0, 0f);
        CameraShake.instance.Shake(0.12f, 0.08f);

        slashLine.SetPosition(0, attackStart);
        slashLine.SetPosition(1, attackStart);
        slashLine.startColor = slashColor;
        slashLine.endColor = slashColor;
        slashLine.enabled = true;

        bool hitExecuted = false;
        float elapsed = 0f;

        while (elapsed < slashDuration)
        {
            float t = elapsed / slashDuration;
            Vector3 tip = Vector3.Lerp(attackStart, attackEnd, t);

            slashLine.SetPosition(1, tip);

            bool active = t >= hitStartTime && t <= hitEndTime;

            // 슬래시 선 색상: 활성 구간에서 주황, 피격 시 빨강, 이외 흰색
            Color lineCol;
            if (hitExecuted)
                lineCol = new Color(1f, 0.1f, 0.1f, 1f - t * 0.4f);
            else if (active)
                lineCol = new Color(1f, 0.5f, 0.1f, 1f - t * 0.3f);
            else
                lineCol = new Color(slashColor.r, slashColor.g, slashColor.b, 1f - t * 0.3f);

            slashLine.startColor = lineCol;
            slashLine.endColor = lineCol;

            // 히트 판정: 플레이어가 현재까지 그어진 선분(attackStart~tip)에서 hitRadius 이내
            if (active && !hitExecuted && playerTransform != null)
            {
                float dist = DistanceToSegment(playerTransform.position, attackStart, tip);
                if (dist <= hitRadius)
                {
                    playerWasInPath = true;
                    hitExecuted = true;
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        slashLine.SetPosition(1, attackEnd);
        yield return new WaitForSecondsRealtime(0.08f);
        slashLine.enabled = false;
        if (animator != null) animator.speed = 1f;
        animator?.Play("Idle", 0, 0f);
        isExecutingAttack = false;
    }
}
