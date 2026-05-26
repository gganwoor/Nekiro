using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI instance;

    public float moveSpeed = 1.5f;
    public float attackRange = 5f;

    [Header("스프라이트 플립")]
    public Transform visualTransform; // EnemyVisual 오브젝트 연결

    private Transform playerTransform;
    private EnemyAttack enemyAttack;

    void Awake() { instance = this; }

    void Start()
    {
        playerTransform = FindObjectOfType<PlayerMovement>()?.transform;
        enemyAttack = GetComponent<EnemyAttack>();
    }

    bool IsActive()
    {
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning) return false;

        // 튜토리얼이 있으면 RealBattle 단계에서만 활성화
        if (TutorialManager.instance != null)
            return TutorialManager.instance.currentStep == TutorialManager.TutorialStep.RealBattle;

        return true;
    }

    void Update()
    {
        if (!IsActive()) return;
        if (playerTransform == null) return;
        if (enemyAttack != null && enemyAttack.IsAttackCycleRunning) return;

        float dist = Mathf.Abs(transform.position.x - playerTransform.position.x);

        if (dist > attackRange)
        {
            float dir = playerTransform.position.x < transform.position.x ? -1f : 1f;
            transform.position += new Vector3(dir * moveSpeed * Time.deltaTime, 0f, 0f);
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null || visualTransform == null) return;

        bool playerOnLeft = playerTransform.position.x < transform.position.x;
        Vector3 scale = visualTransform.localScale;
        scale.x = playerOnLeft ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        visualTransform.localScale = scale;
    }
}
