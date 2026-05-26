using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;

    public float moveSpeed = 3f;

    [Header("이동 범위 (전투 모드)")]
    public float minX = -20f;
    public float maxX = 100f;

    public bool isTravelMode = false;

    private Animator animator;
    private bool facingLeft = false;
    public bool FacingLeft => facingLeft;

    void Awake() { instance = this; }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (TutorialManager.inputBlocked)
        {
            animator.SetBool("Run", false);
            return;
        }

        if (isTravelMode)
            UpdateTravelMode();
        else
            UpdateBattleMode();
    }

    void UpdateTravelMode()
    {
        float h = Input.GetAxisRaw("Horizontal");

        if (h != 0)
            WorldScroller.instance?.Scroll(h * moveSpeed * Time.deltaTime);

        animator.SetBool("Run", h != 0);

        if (h != 0)
            facingLeft = h < 0;
    }

    void UpdateBattleMode()
    {
        bool holdingBreath = BattleManager.instance != null && BattleManager.instance.IsHoldingBreath;
        bool attacking = PlayerAttack.instance != null && PlayerAttack.instance.IsAttacking;
        bool dashing = PlayerDash.instance != null && PlayerDash.instance.IsDashing;

        if (holdingBreath || attacking || dashing)
        {
            animator.SetBool("Run", false);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");

        float x = transform.position.x;
        x = Mathf.Clamp(x + h * moveSpeed * Time.deltaTime, minX, maxX);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        animator.SetBool("Run", h != 0);

        if (h != 0)
            facingLeft = h < 0;
    }

    void LateUpdate()
    {
        Vector3 scale = transform.localScale;
        scale.x = facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
