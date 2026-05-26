using System.Collections;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    public static PlayerDash instance;

    public float dashDistance = 3f;
    public float dashDuration = 0.18f;
    public float invincibleDuration = 0.28f;
    public float cooldown = 1.5f;
    public KeyCode dashKey = KeyCode.LeftShift;

    public bool IsDashing => isDashing;
    public bool IsInvincible => isInvincible;

    private bool isDashing = false;
    private bool isInvincible = false;
    private float cooldownTimer = 0f;

    void Awake() { instance = this; }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (TutorialManager.inputBlocked) return;
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning) return;
        if (BattleManager.instance.IsHoldingBreath) return;
        if (PlayerAttack.instance != null && PlayerAttack.instance.IsAttacking) return;
        if (isDashing || cooldownTimer > 0f) return;

        if (Input.GetKeyDown(dashKey))
        {
            float dir = Input.GetAxisRaw("Horizontal");
            if (dir == 0f)
                dir = (PlayerMovement.instance != null && PlayerMovement.instance.FacingLeft) ? -1f : 1f;
            StartCoroutine(DashRoutine(dir));
        }
    }

    IEnumerator DashRoutine(float direction)
    {
        isDashing = true;
        isInvincible = true;

        float minX = PlayerMovement.instance != null ? PlayerMovement.instance.minX : -20f;
        float maxX = PlayerMovement.instance != null ? PlayerMovement.instance.maxX : 100f;

        Vector3 startPos = transform.position;
        float targetX = Mathf.Clamp(startPos.x + direction * dashDistance, minX, maxX);
        Vector3 endPos = new Vector3(targetX, startPos.y, startPos.z);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dashDuration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;
        isDashing = false;

        yield return new WaitForSeconds(invincibleDuration - dashDuration);
        isInvincible = false;

        cooldownTimer = cooldown;
    }
}
