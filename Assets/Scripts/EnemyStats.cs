using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("스탯")]
    public float maxHP = 100f;
    public float currentHP;
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("스태미나 회복")]
    public float staminaRegenRate = 5f;
    public float regenDelay = 2f;

    [Header("그로기")]
    public float groggyDuration = 5f;

    private float regenTimer = 0f;
    private Animator animator;
    private bool isDead = false;
    private bool isGroggy = false;
    public bool IsGroggy => isGroggy;

    [Header("UI 연결")]
    public RectTransform hpBar;
    public RectTransform staminaBar;

    void Start()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        animator = GetComponentInChildren<Animator>(true);
        UpdateUI();
    }

    void Update()
    {
        if (isDead || isGroggy) return;

        if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
            UpdateUI();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        float prev = currentHP;
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        UpdateUI();

        if (prev > 0 && currentHP <= 0)
        {
            isDead = true;
            PlayDeath();

            if (TutorialManager.instance != null &&
                TutorialManager.instance.currentStep == TutorialManager.TutorialStep.RealBattle)
                TutorialManager.instance.CompleteStep();
            else
                BattleManager.instance?.OnEnemyDead();
        }
    }

    void PlayDeath()
    {
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) { ai.StopAllCoroutines(); ai.enabled = false; }

        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null) { attack.StopAllCoroutines(); attack.enabled = false; }

        if (animator != null)
        {
            animator.Play("Death", 0, 0f);
            StartCoroutine(FreezeOnDeathEnd());
        }
    }

    IEnumerator FreezeOnDeathEnd()
    {
        yield return null; // Play()가 적용될 때까지 한 프레임 대기
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Death"))
            {
                if (info.normalizedTime >= 0.95f)
                {
                    animator.speed = 0f; // 마지막 프레임에서 정지
                    yield break;
                }
            }
            else
            {
                animator.Play("Death", 0, 0f); // 다른 상태로 빠져나가면 강제 복귀
            }
            yield return null;
        }
    }

    public void ResetStats()
    {
        StopAllCoroutines();
        currentHP = maxHP;
        currentStamina = maxStamina;
        regenTimer = 0f;
        isDead = false;
        isGroggy = false;
        if (animator != null) animator.speed = 1f;
        UpdateUI();
    }

    public void UseStamina(float amount)
    {
        if (isDead || isGroggy) return;
        float prev = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
        regenTimer = regenDelay;
        UpdateUI();

        if (prev > 0 && currentStamina <= 0)
            StartCoroutine(GroggyRoutine());
    }

    IEnumerator GroggyRoutine()
    {
        isGroggy = true;

        EnemyAttack attack = GetComponent<EnemyAttack>();
        attack?.InterruptAttack();

        animator?.Play("Groggy", 0, 0f);

        float elapsed = 0f;
        while (elapsed < groggyDuration)
        {
            if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Groggy"))
                animator.Play("Groggy", 0, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        currentStamina = maxStamina;
        regenTimer = 0f;
        isGroggy = false;
        animator?.Play("Idle", 0, 0f);
        UpdateUI();
    }

    public void RecoverStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (hpBar != null)
            hpBar.localScale = new Vector3(currentHP / maxHP, 1f, 1f);
        if (staminaBar != null)
            staminaBar.localScale = new Vector3(currentStamina / maxStamina, 1f, 1f);
    }
}