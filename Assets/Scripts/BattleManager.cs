using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    public enum BattlePhase { Idle, Battle }
    public BattlePhase currentPhase = BattlePhase.Idle;

    [Header("전투 시작 대기")]
    public float idleTime = 3f;

    [Header("테스트")]
    public bool autoPhaseEnabled = true;

    [Header("슬로우 모션")]
    public float slowMotionScale = 0.15f;

    [Header("카메라 줌")]
    public float zoomedCamSize = 3f;
    public Vector2 zoomedCamOffset = new Vector2(0f, 0.5f);
    public float transitionDuration = 0.6f;
    public float zoomOutDuration = 0.3f;

    [Header("화면 어둡기")]
    public Image darkOverlay;
    public float darkOverlayAlpha = 0.4f;

    [Header("숨 참기")]
    public float holdBreathDrainRate = 15f;

    [Header("플레이어 공격")]
    public KeyCode attackKey = KeyCode.Space;

    private Vector3 zoomedCamPos;
    private TrajectoryDrawer trajectoryDrawer;
    private PlayerStats playerStats;
    private Transform playerTransform;
    private Camera mainCamera;
    private float originalCamSize;
    private Vector3 originalCamPos;
    private bool battleEnded = false;
    private bool isBattleActive = false;
    private bool isBattleRunning = false;
    private bool isHoldingBreath = false;
    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    public bool IsBattleActive => isBattleActive;
    public bool IsBattleRunning => isBattleRunning;
    public bool IsHoldingBreath => isHoldingBreath;
    public bool battleCycleComplete = false;

    void Awake() { instance = this; }

    void Start()
    {
        trajectoryDrawer = FindObjectOfType<TrajectoryDrawer>();
        playerStats = FindObjectOfType<PlayerStats>();
        playerTransform = FindObjectOfType<PlayerMovement>()?.transform;

        mainCamera = Camera.main;
        originalCamSize = mainCamera.orthographicSize;
        originalCamPos = mainCamera.transform.position;

        StartCoroutine(BattleLoop());
    }

    public void StartBattle()
    {
        if (isBattleRunning || isBattleActive || battleEnded) return;
        isBattleActive = true;
    }

    void Update()
    {
        if (battleEnded) return;

        if (autoPhaseEnabled && !isBattleRunning && currentPhase == BattlePhase.Idle)
            StartBattle();

        if (currentPhase != BattlePhase.Battle) return;

        // 숨 참기 진입
        if (Input.GetMouseButtonDown(1) && !isHoldingBreath && !isTransitioning)
        {
            bool canAttack = PlayerAttack.instance == null || !PlayerAttack.instance.IsAttacking;
            if (canAttack && (playerStats == null || playerStats.currentStamina > 0))
                StartHoldBreath();
        }

        // 숨 참기 해제
        if (Input.GetMouseButtonUp(1) && isHoldingBreath)
            EndHoldBreath();

        // 스태미나 소모
        if (isHoldingBreath && playerStats != null)
        {
            playerStats.UseStamina(holdBreathDrainRate * Time.unscaledDeltaTime);
            if (playerStats.currentStamina <= 0)
                EndHoldBreath();
        }

        // 공격 키 (숨 참기 중에는 불가)
        if (!isHoldingBreath && !isTransitioning && Input.GetKeyDown(attackKey))
            PlayerAttack.instance?.TriggerAttack();
    }

    void StartHoldBreath()
    {
        isHoldingBreath = true;
        originalCamPos = mainCamera.transform.position;
        originalCamSize = mainCamera.orthographicSize;
        if (playerTransform != null)
            zoomedCamPos = new Vector3(playerTransform.position.x + zoomedCamOffset.x,
                                       playerTransform.position.y + zoomedCamOffset.y, -10f);

        PlayerAttack.instance?.SetPreparePose(true);
        trajectoryDrawer.SetDrawingEnabled(true);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionToDesign());
    }

    public void EndHoldBreath()
    {
        isHoldingBreath = false;
        trajectoryDrawer.SetDrawingEnabled(false);
        PlayerAttack.instance?.SetPreparePose(false);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionToNormal());
    }

    public void OnEnemyDead()
    {
        if (battleEnded) return;
        battleEnded = true;
        isBattleRunning = false;
        isHoldingBreath = false;
        StopAllCoroutines();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0f);
        StartCoroutine(BattleEndRoutine(true));
    }

    public void OnPlayerDead()
    {
        if (battleEnded) return;
        battleEnded = true;
        isBattleRunning = false;
        isHoldingBreath = false;
        StopAllCoroutines();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0f);
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
            isBattleRunning = false;
            if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = autoPhaseEnabled;
            if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = true;
            trajectoryDrawer.SetDrawingEnabled(false);

            yield return new WaitUntil(() => isBattleActive);
            isBattleActive = false;

            currentPhase = BattlePhase.Battle;
            isBattleRunning = true;
            if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = false;
            if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = false;
            yield return new WaitForSecondsRealtime(idleTime);

            // EnemyAttack이 자체 타이머로 공격 관리
            while (!battleEnded) yield return null;
        }
    }

    IEnumerator TransitionToDesign()
    {
        isTransitioning = true;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            mainCamera.orthographicSize = Mathf.Lerp(originalCamSize, zoomedCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(originalCamPos, zoomedCamPos, t);
            Time.timeScale = Mathf.Lerp(1f, slowMotionScale, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, darkOverlayAlpha, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.orthographicSize = zoomedCamSize;
        mainCamera.transform.position = zoomedCamPos;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, darkOverlayAlpha);
        isTransitioning = false;
    }

    IEnumerator TransitionToNormal()
    {
        isTransitioning = true;
        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration);
            mainCamera.orthographicSize = Mathf.Lerp(zoomedCamSize, originalCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(zoomedCamPos, originalCamPos, t);
            Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(darkOverlayAlpha, 0f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.orthographicSize = originalCamSize;
        mainCamera.transform.position = originalCamPos;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0f);
        isTransitioning = false;
    }
}
