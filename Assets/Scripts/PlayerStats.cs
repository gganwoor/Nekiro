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

        if (hpBar != null)
        {
            hpBar.type = UnityEngine.UI.Image.Type.Filled;
            hpBar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            hpBar.fillOrigin = 0;
        }
        if (staminaBar != null)
        {
            staminaBar.type = UnityEngine.UI.Image.Type.Filled;
            staminaBar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            staminaBar.fillOrigin = 0;
        }

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
            hpBar.fillAmount = currentHP / maxHP;
        if (staminaBar != null)
            staminaBar.fillAmount = currentStamina / maxStamina;
    }
}