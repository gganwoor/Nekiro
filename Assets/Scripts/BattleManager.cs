using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    public enum BattlePhase { Idle, Design, Execute, Result }
    public BattlePhase currentPhase = BattlePhase.Idle;

    [Header("페이즈 시간")]
    public float idleTime = 3f;
    public float designTime = 4f;

    [Header("슬로우 모션")]
    public float slowMotionScale = 0.15f;

    [Header("카메라 줌")]
    public float zoomedCamSize = 3f;
    public Vector3 zoomedCamPos = new Vector3(-3f, -0.5f, -10f);
    public float transitionDuration = 0.6f;

    [Header("플레이어 공격")]
    public KeyCode attackKey = KeyCode.Space;
    public float playerAttackDamage = 20f;

    private EnemyAttack enemyAttack;
    private TrajectoryDrawer trajectoryDrawer;
    private EnemyStats enemyStats;
    private Animator playerAnimator;
    private Camera mainCamera;
    private float originalCamSize;
    private Vector3 originalCamPos;
    private bool battleEnded = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        enemyAttack = FindObjectOfType<EnemyAttack>();
        trajectoryDrawer = FindObjectOfType<TrajectoryDrawer>();
        enemyStats = FindObjectOfType<EnemyStats>();

        PlayerStats ps = FindObjectOfType<PlayerStats>();
        if (ps != null)
            playerAnimator = ps.GetComponentInChildren<Animator>();

        mainCamera = Camera.main;
        originalCamSize = mainCamera.orthographicSize;
        originalCamPos = mainCamera.transform.position;

        foreach (TutorialManager tm in FindObjectsOfType<TutorialManager>())
            tm.enabled = false;

        StartCoroutine(BattleLoop());
    }

    void Update()
    {
        if (battleEnded) return;

        if (currentPhase == BattlePhase.Idle && Input.GetKeyDown(attackKey))
        {
            if (enemyStats != null)
            {
                if (enemyStats.currentStamina > 0)
                    enemyStats.UseStamina(playerAttackDamage);
                else
                    enemyStats.TakeDamage(playerAttackDamage);

                EnemyHitFlash.instance?.Flash();
                CameraShake.instance.Shake(0.1f, 0.08f);
            }
        }
    }

    public void OnEnemyDead()
    {
        if (battleEnded) return;
        battleEnded = true;
        StopAllCoroutines();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        StartCoroutine(BattleEndRoutine(true));
    }

    public void OnPlayerDead()
    {
        if (battleEnded) return;
        battleEnded = true;
        StopAllCoroutines();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        StartCoroutine(BattleEndRoutine(false));
    }

    IEnumerator BattleEndRoutine(bool victory)
    {
        CameraShake.instance.Shake(victory ? 0.2f : 0.5f, victory ? 0.15f : 0.3f);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator BattleLoop()
    {
        while (true)
        {
            currentPhase = BattlePhase.Idle;
            trajectoryDrawer.SetDrawingEnabled(false);
            yield return new WaitForSeconds(idleTime);

            enemyAttack.StartAttack();
            yield return StartCoroutine(TransitionToDesign());

            currentPhase = BattlePhase.Design;
            trajectoryDrawer.ResetJudgement();
            trajectoryDrawer.SetDrawingEnabled(true);

            float elapsed = 0f;
            while (elapsed < designTime && !trajectoryDrawer.isReadyToJudge)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            trajectoryDrawer.SetDrawingEnabled(false);

            yield return StartCoroutine(TransitionToNormal());

            currentPhase = BattlePhase.Execute;
            trajectoryDrawer.ExecuteJudgement();
            yield return new WaitForSeconds(1f);

            currentPhase = BattlePhase.Result;
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator TransitionToDesign()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool("Prepare", true);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            mainCamera.orthographicSize = Mathf.Lerp(originalCamSize, zoomedCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(originalCamPos, zoomedCamPos, t);
            Time.timeScale = Mathf.Lerp(1f, slowMotionScale, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.orthographicSize = zoomedCamSize;
        mainCamera.transform.position = zoomedCamPos;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;
    }

    IEnumerator TransitionToNormal()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool("Prepare", false);

        float elapsed = 0f;
        float halfDuration = transitionDuration * 0.5f;
        while (elapsed < halfDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            mainCamera.orthographicSize = Mathf.Lerp(zoomedCamSize, originalCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(zoomedCamPos, originalCamPos, t);
            Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.orthographicSize = originalCamSize;
        mainCamera.transform.position = originalCamPos;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
