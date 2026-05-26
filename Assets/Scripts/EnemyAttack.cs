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

    private LineRenderer warningLine;
    private LineRenderer slashLine;
    private Transform playerTransform;
    private TrajectoryDrawer trajectoryDrawer;
    private int lastPatternIndex = -1;
    private bool isWarningActive = false;
    private float attackTimer = 0f;
    private bool attackCycleRunning = false;
    private bool battleWasRunning = false;

    void Start()
    {
        playerTransform = FindObjectOfType<PlayerMovement>()?.transform;
        trajectoryDrawer = FindObjectOfType<TrajectoryDrawer>();

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

        // 전투 시작 시 타이머 초기화
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

        attackTimer += Time.unscaledDeltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartCoroutine(AttackCycle());
        }
    }

    IEnumerator AttackCycle()
    {
        attackCycleRunning = true;

        SelectPattern();
        trajectoryDrawer?.ResetJudgement();

        yield return StartCoroutine(ShowWarning());
        yield return StartCoroutine(ExecuteAttack());

        trajectoryDrawer?.ExecuteJudgement();
        yield return new WaitForSecondsRealtime(0.8f);

        if (BattleManager.instance != null)
            BattleManager.instance.battleCycleComplete = true;

        attackCycleRunning = false;
    }

    void SelectPattern()
    {
        if (patterns.Length == 0) return;

        int index;
        do { index = Random.Range(0, patterns.Length); }
        while (index == lastPatternIndex && patterns.Length > 1);
        lastPatternIndex = index;

        AttackPattern p = patterns[index];
        Vector3 origin = transform.position;

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
        isWarningActive = true;
        warningLine.enabled = false;

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (trajectoryDrawer != null && trajectoryDrawer.isReadyToJudge)
                break;

            bool holding = BattleManager.instance != null && BattleManager.instance.IsHoldingBreath;
            warningLine.enabled = holding;
            if (holding)
            {
                float alpha = Mathf.PingPong(elapsed * 3f, 1f);
                warningLine.startColor = new Color(warningColor.r, warningColor.g, warningColor.b, alpha);
                warningLine.endColor = new Color(warningColor.r, warningColor.g, warningColor.b, alpha);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 궤적을 그리지 않은 경우: 공격 직전에 짧게 예고선 표시
        if (trajectoryDrawer == null || !trajectoryDrawer.isReadyToJudge)
        {
            warningLine.startColor = warningColor;
            warningLine.endColor = warningColor;
            warningLine.enabled = true;
            yield return new WaitForSecondsRealtime(briefWarningDuration);
        }

        isWarningActive = false;
        warningLine.enabled = false;
    }

    IEnumerator ExecuteAttack()
    {
        isWarningActive = false;
        warningLine.enabled = false;

        slashLine.SetPosition(0, attackStart);
        slashLine.SetPosition(1, attackStart);
        slashLine.startColor = slashColor;
        slashLine.endColor = slashColor;
        slashLine.enabled = true;

        float elapsed = 0f;
        while (elapsed < slashDuration)
        {
            float t = elapsed / slashDuration;
            slashLine.SetPosition(1, Vector3.Lerp(attackStart, attackEnd, t));

            float alpha = 1f - t * 0.3f;
            Color c = new Color(slashColor.r, slashColor.g, slashColor.b, alpha);
            slashLine.startColor = c;
            slashLine.endColor = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        slashLine.SetPosition(1, attackEnd);
        yield return new WaitForSeconds(0.08f);
        slashLine.enabled = false;
    }
}
