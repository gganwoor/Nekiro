using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("스탯")]
    public float maxHP = 100f;
    public float currentHP;
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("스태미나 회복")]
    public float staminaRegenRate = 8f;

    [Header("UI연결")]
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
            BattleManager.instance?.OnPlayerDead();
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
        UpdateUI();
    }

    public void RecoverStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateUI();
    }

    void Update()
    {
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