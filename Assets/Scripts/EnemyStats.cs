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

    private float regenTimer = 0f;

    [Header("UI 연결")]
    public RectTransform hpBar;
    public RectTransform staminaBar;

    void Start()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        UpdateUI();
    }

    void Update()
    {
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
        float prev = currentHP;
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        UpdateUI();

        if (prev > 0 && currentHP <= 0)
        {
            if (TutorialManager.instance != null &&
                TutorialManager.instance.currentStep == TutorialManager.TutorialStep.RealBattle)
                TutorialManager.instance.CompleteStep();
            else
                BattleManager.instance?.OnEnemyDead();
        }
    }

    public void UseStamina(float amount)
    {
        float prev = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
        regenTimer = regenDelay;
        UpdateUI();

        if (prev > 0 && currentStamina <= 0 &&
            TutorialManager.instance != null &&
            TutorialManager.instance.currentStep == TutorialManager.TutorialStep.StaminaPractice)
        {
            TutorialManager.instance.CompleteStep();
        }
    }

    public void RecoverStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateUI();
    }

    public void ResetStats()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        regenTimer = 0f;
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