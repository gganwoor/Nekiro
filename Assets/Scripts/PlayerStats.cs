using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("스탯")]
    public float maxHP = 100f;
    public float currentHP;
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("UI연결")]
    public Image hpBar;
    public Image staminaBar;

    void Start()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        UpdateUI();
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
            hpBar.fillAmount = currentHP / maxHP;
        if (staminaBar != null)
            staminaBar.fillAmount = currentStamina / maxStamina;
    }
}