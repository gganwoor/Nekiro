using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI instance;

    public float moveSpeed = 2.5f;
    public float attackRange = 5f;
    public float hitStaggerDuration = 0.4f;

    [Header("넉백")]
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.15f;

    [Header("스프라이트 플립")]
    public Transform visualTransform;

    [Header("에스컬레이션")]
    public bool escalationEnabled = true;
    public float maxMoveSpeed = 4.5f;

    private Transform playerTransform;
    private EnemyAttack enemyAttack;
    private EnemyStats enemyStats;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isHit = false;
    private float lastMoveDir = -1f;

    void Awake() { instance = this; }

    void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;
        enemyAttack = GetComponent<EnemyAttack>();
        enemyStats = GetComponent<EnemyStats>();
        animator = GetComponentInChildren<Animator>(true);
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    bool IsActive()
    {
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning) return false;
        if (enemyStats != null && enemyStats.IsGroggy) return false;

        if (TutorialManager.instance != null)
        {
            var step = TutorialManager.instance.currentStep;
            return step >= TutorialManager.TutorialStep.DrawTrajectory &&
                   step < TutorialManager.TutorialStep.Clear;
        }

        return true;
    }

    void Update()
    {
        bool moving = false;

        if (IsActive() && !isHit && playerTransform != null && (enemyAttack == null || !enemyAttack.IsExecutingAttack))
        {
            Vector3 referencePos = visualTransform != null ? visualTransform.position : transform.position;
            float dist = Mathf.Abs(referencePos.x - playerTransform.position.x);
            if (dist > attackRange)
            {
                float dir = playerTransform.position.x < referencePos.x ? -1f : 1f;
                float speed = moveSpeed;
                if (escalationEnabled && enemyStats != null && enemyStats.maxHP > 0f)
                {
                    float ratio = 1f - Mathf.Clamp01(enemyStats.currentHP / enemyStats.maxHP);
                    speed = Mathf.Lerp(moveSpeed, maxMoveSpeed, ratio);
                }
                transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
                lastMoveDir = dir;
                moving = true;
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = lastMoveDir < 0f;

        animator?.SetBool("Run", moving);
    }

    public void OnHit()
    {
        StartCoroutine(HitStaggerRoutine());
    }

    IEnumerator HitStaggerRoutine()
    {
        isHit = true;
        animator?.Play("Hit", 0, 0f);
        StartCoroutine(KnockbackRoutine());
        yield return new WaitForSecondsRealtime(hitStaggerDuration);
        animator?.Play("Idle", 0, 0f);
        isHit = false;
    }

    IEnumerator KnockbackRoutine()
    {
        float referenceX = visualTransform != null ? visualTransform.position.x : transform.position.x;
        float dir = playerTransform != null && playerTransform.position.x < referenceX ? 1f : -1f;
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(dir * knockbackDistance, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.position = end;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 referencePos = visualTransform != null ? visualTransform.position : transform.position;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(referencePos, attackRange);
    }

}
