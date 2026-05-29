using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public static PlayerAttack instance;

    [Header("히트박스")]
    public Transform hitboxPoint;
    public Vector2 hitboxSize = new Vector2(0.8f, 0.5f);
    public LayerMask enemyLayer;

    [Header("데미지")]
    public float hpDamage = 20f;
    public float blockedHpDamage = 5f;

    [Header("공격 스태미나 소모")]
    public float attackStaminaCost = 10f;

    [Header("히트 타이밍 (0~1, 애니메이션 재생 비율)")]
    public float hitStartTime = 0.3f;
    public float hitEndTime = 0.6f;

    [Header("준비 자세 애니메이션")]
    public string preparePoseClip = "Prepare";

    [Header("패링 애니메이션")]
    public string parryClip = "Parry";
    public float parryHoldTime = 0.5f;
    public AudioClip parrySound;

    [Header("피격 애니메이션")]
    public string hitClip = "Hit";
    public float hitHoldTime = 0.5f;

    [Header("애니메이션 속도")]
    public float attackAnimSpeed = 1.6f;

    private Animator animator;
    private AudioSource audioSource;
    private PlayerStats playerStats;
    private bool isAttacking = false;
    private bool isHit = false;
    public bool IsHit => isHit;
    private bool hitExecuted = false;
    private bool attackQueued = false;
    private int transitionBlockFrame = -1;
    private LineRenderer hitboxVisual;
    private string currentAttackClip = "Attack1";
    private static readonly string[] attackClips = { "Attack1", "Attack2" };

    void Awake() { instance = this; }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        playerStats = GetComponentInParent<PlayerStats>();
        BuildHitboxVisual();
    }

    void BuildHitboxVisual()
    {
        if (hitboxPoint == null) return;

        GameObject go = new GameObject("HitboxVisual");
        go.transform.SetParent(hitboxPoint);
        go.transform.localPosition = Vector3.zero;

        hitboxVisual = go.AddComponent<LineRenderer>();
        hitboxVisual.useWorldSpace = false;
        hitboxVisual.loop = true;
        hitboxVisual.startWidth = 0.03f;
        hitboxVisual.endWidth = 0.03f;
        hitboxVisual.material = new Material(Shader.Find("Sprites/Default"));
        hitboxVisual.sortingOrder = 10;
        hitboxVisual.positionCount = 4;

        UpdateHitboxVisualShape();
        SetHitboxVisualColor(new Color(1f, 0.5f, 0f, 0.8f));
    }

    void UpdateHitboxVisualShape()
    {
        if (hitboxVisual == null) return;
        float hw = hitboxSize.x * 0.5f;
        float hh = hitboxSize.y * 0.5f;
        hitboxVisual.SetPosition(0, new Vector3(-hw, -hh, 0f));
        hitboxVisual.SetPosition(1, new Vector3( hw, -hh, 0f));
        hitboxVisual.SetPosition(2, new Vector3( hw,  hh, 0f));
        hitboxVisual.SetPosition(3, new Vector3(-hw,  hh, 0f));
    }

    void SetHitboxVisualColor(Color color)
    {
        if (hitboxVisual == null) return;
        hitboxVisual.startColor = color;
        hitboxVisual.endColor = color;
    }

    public bool IsAttacking => isAttacking;

    void ResetAttackState()
    {
        isAttacking = false;
        attackQueued = false;
        hitExecuted = false;
    }

    public void PlayParry(bool withSound = true)
    {
        ResetAttackState();
        animator.speed = 1f;
        animator.Play(parryClip, 0, 0f);
        if (withSound && parrySound != null) audioSource.PlayOneShot(parrySound);
        StartCoroutine(WaitForParryEnd());
    }

    public void PlayHit()
    {
        ResetAttackState();
        animator.speed = 1f;
        isHit = true;
        animator.Play(hitClip, 0, 0f);
        StartCoroutine(WaitForHitEnd());
    }

    IEnumerator WaitForHitEnd()
    {
        yield return null;
        AnimatorStateInfo state;
        do
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        } while (state.IsName(hitClip) && state.normalizedTime < 1f);

        if (state.IsName(hitClip))
        {
            yield return new WaitForSeconds(hitHoldTime);
            animator.Play("Idle", 0, 0f);
        }
        isHit = false;
    }

    IEnumerator WaitForParryEnd()
    {
        yield return null;
        AnimatorStateInfo state;
        do
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        } while (state.IsName(parryClip) && state.normalizedTime < 1f);
        if (state.IsName(parryClip))
        {
            yield return new WaitForSeconds(parryHoldTime);
            animator.Play("Idle", 0, 0f);
        }
    }

    public void PlayCounterAnim()
    {
        StopAllCoroutines();
        ResetAttackState();
        isAttacking = true;
        hitExecuted = true;
        currentAttackClip = attackClips[0];
        animator.speed = attackAnimSpeed;
        animator.Play(currentAttackClip, 0, 0f);
    }

    public void SetPreparePose(bool active)
    {
        if (active)
            animator.Play(preparePoseClip, 0, 0f);
        else
            animator.Play("Idle", 0, 0f);
    }

    public void TriggerAttack()
    {
        if (!isAttacking)
        {
            playerStats?.UseStamina(attackStaminaCost);
            isAttacking = true;
            hitExecuted = false;
            currentAttackClip = attackClips[0];
            animator.speed = attackAnimSpeed;
            animator.Play(currentAttackClip, 0, 0f);
        }
        else if (!attackQueued && Time.frameCount != transitionBlockFrame)
        {
            playerStats?.UseStamina(attackStaminaCost);
            attackQueued = true;
        }
    }

    void Update()
    {
        UpdateHitboxVisualShape();

        if (!isAttacking) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(currentAttackClip)) return;

        float t = state.normalizedTime;

        if (!hitExecuted && t >= hitStartTime && t <= hitEndTime)
        {
            hitExecuted = true;
            SetHitboxVisualColor(Color.red);
            OnAttackHit();
        }

        if (t >= 1f)
        {
            if (attackQueued)
            {
                transitionBlockFrame = Time.frameCount;
                attackQueued = false;
                hitExecuted = false;
                currentAttackClip = currentAttackClip == attackClips[0] ? attackClips[1] : attackClips[0];
                animator.speed = attackAnimSpeed;
                animator.Play(currentAttackClip, 0, 0f);
            }
            else
            {
                OnAttackEnd();
            }
        }
    }

    void OnAttackHit()
    {
        if (hitboxPoint == null) return;

        Collider2D hit = Physics2D.OverlapBox(hitboxPoint.position, hitboxSize, 0f, enemyLayer);
        if (hit == null) return;

        TutorialScarecrow scarecrow = hit.GetComponentInParent<TutorialScarecrow>();
        if (scarecrow != null)
        {
            scarecrow.OnHit();
            CameraShake.instance?.Shake(0.1f, 0.07f);
            HitStop.instance?.Stop(0.05f);
            return;
        }

        EnemyStats stats = hit.GetComponentInParent<EnemyStats>();
        if (stats == null) return;

        float dmg = stats.IsGroggy ? hpDamage : blockedHpDamage;
        stats.TakeDamage(dmg);

        EnemyHitFlash.instance?.Flash();
        CameraShake.instance.Shake(0.1f, 0.08f);
        EnemyAI.instance?.OnHit();
    }

    void OnAttackEnd()
    {
        isAttacking = false;
        hitExecuted = false;
        attackQueued = false;
        animator.speed = 1f;
        animator.Play("Idle", 0, 0f);
        SetHitboxVisualColor(new Color(1f, 0.5f, 0f, 0.8f));
    }
}
