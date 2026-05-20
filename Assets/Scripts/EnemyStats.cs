using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("스탯")]
    public float maxHP = 100f;
    public float currentHP;
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("UI 연결")]
    public RectTransform hpBar;
    public RectTransform staminaBar;

    void Start()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        UpdateUI();
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

    void UpdateUI()
    {
        if (hpBar != null)
            hpBar.localScale = new Vector3(currentHP / maxHP, 1f, 1f);
        if (staminaBar != null)
            staminaBar.localScale = new Vector3(currentStamina / maxStamina, 1f, 1f);
    }
}