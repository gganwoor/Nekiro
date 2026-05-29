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

    [Header("반격 창문")]
    public float counterWindowDuration = 0.7f;
    public float counterHpDamage = 50f;
    public float counterBlockedHpDamage = 15f;

    private Vector3 zoomedCamPos;
    private TrajectoryDrawer trajectoryDrawer;
    private PlayerStats playerStats;
    private EnemyStats enemyStats;
    private Transform playerTransform;
    private Camera mainCamera;
    private float originalCamSize;
    private Vector3 originalCamPos;
    private bool battleEnded = false;
    private bool isBattleActive = false;
    private bool isBattleRunning = false;
    private bool isHoldingBreath = false;
    private bool isTransitioning = false;
    private bool isCounterWindowOpen = false;
    private float counterWindowTimer = 0f;
    private Coroutine transitionCoroutine;

    public bool IsBattleActive => isBattleActive;
    public bool IsBattleRunning => isBattleRunning;
    public bool IsHoldingBreath => isHoldingBreath;
    public bool IsCounterWindowOpen => isCounterWindowOpen;
    public bool battleCycleComplete = false;

    void Awake() 
{ 
    instance = this; 

    Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
    foreach (Camera cam in allCameras)
    {
        if (cam.gameObject.scene.name == "DontDestroyOnLoad")
        {
            Destroy(cam.gameObject);
        }
    }
}


    void Start()
    {
        trajectoryDrawer = FindFirstObjectByType<TrajectoryDrawer>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        enemyStats = FindFirstObjectByType<EnemyStats>();
        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;

        // [수정] 카메라를 안전하게 찾아오는 함수로 분리해서 호출합니다.
        AssignMainCamera();

        StartCoroutine(BattleLoop());
    }

    // [추가] 메인 카메라와 초기 값을 안전하게 할당하는 메서드
    void AssignMainCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCamSize = mainCamera.orthographicSize;
                originalCamPos = mainCamera.transform.position;
            }
        }
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

        // 숨 참기 토글
        if (Input.GetMouseButtonDown(1) && !isTransitioning)
        {
            if (!isHoldingBreath)
            {
                bool canAttack = PlayerAttack.instance == null || !PlayerAttack.instance.IsAttacking;
                if (canAttack && (playerStats == null || playerStats.currentStamina > 0))
                    StartHoldBreath();
            }
            else
            {
                EndHoldBreath();
            }
        }

        // 스태미나 소모
        if (isHoldingBreath && playerStats != null)
        {
            playerStats.UseStamina(holdBreathDrainRate * Time.unscaledDeltaTime);
            if (playerStats.currentStamina <= 0)
                EndHoldBreath();
        }

        // 반격 창문 타임아웃
        if (isCounterWindowOpen)
        {
            counterWindowTimer -= Time.unscaledDeltaTime;
            if (counterWindowTimer <= 0f)
            {
                isCounterWindowOpen = false;
                Debug.Log("[Counter] 반격 창문 시간 초과 — 기회 놓침");
            }
        }

        // 공격 키
        if (!isHoldingBreath && Input.GetKeyDown(attackKey))
        {
            Debug.Log($"[Counter] 스페이스 입력 — isCounterWindowOpen: {isCounterWindowOpen}, isTransitioning: {isTransitioning}");
            if (isCounterWindowOpen)
                ExecuteCounter();
            else if (!isTransitioning)
                PlayerAttack.instance?.TriggerAttack();
        }
    }

    public void OpenCounterWindow()
    {
        if (battleEnded) return;
        isCounterWindowOpen = true;
        counterWindowTimer = counterWindowDuration;
        Debug.Log("[Counter] 반격 창문 열림 — 스페이스로 카운터 가능");
        HitStop.instance?.Stop(0.18f);
        if (TutorialManager.instance != null &&
            TutorialManager.instance.currentStep == TutorialManager.TutorialStep.Counter)
            TutorialManager.instance.ShowText("패링 성공! Space로 카운터 반격하세요!");
    }

    void ExecuteCounter()
    {
        isCounterWindowOpen = false;
        Debug.Log("[Counter] 카운터 발동!");
        if (enemyStats == null) enemyStats = FindFirstObjectByType<EnemyStats>();
        Debug.Log($"[Counter] enemyStats: {(enemyStats == null ? "NULL" : "OK")}, stamina: {enemyStats?.currentStamina}");
        if (enemyStats != null)
        {
            float dmg = enemyStats.IsGroggy ? counterHpDamage : counterBlockedHpDamage; // 오타가 있을 수 있어 기존 필드명 유지 (IsGroggy 혹은 IsGgrogy)
            enemyStats.TakeDamage(dmg);
        }
        PlayerAttack.instance?.PlayCounterAnim();
        EnemyHitFlash.instance?.Flash();
        CounterFlash.instance?.Play();
        CameraShake.instance?.Shake(0.45f, 0.35f);
        HitStop.instance?.Stop(0.22f);
        EnemyAI.instance?.OnHit();
        if (TutorialManager.instance != null &&
            TutorialManager.instance.currentStep == TutorialManager.TutorialStep.Counter)
            TutorialManager.instance.CompleteStep();
    }

    void StartHoldBreath()
    {
        // [수정] 씬 1에서 넘어와서 카메라를 유실했을 경우를 대비해 다시 확인합니다.
        AssignMainCamera();
        if (mainCamera == null)
        {
            Debug.LogError("❌ [BattleManager] 메인 카메라를 찾을 수 없어 줌을 실행할 수 없습니다.");
            return;
        }

        isHoldingBreath = true;

        // [수정] 씬 전환 직후의 카메라 꼬임 방지를 위해, 줌이 켜지는 '현재 시점'의 값을 오리지널 값으로 갱신합니다.
        Vector3 shakeOffset = CameraShake.instance != null ? CameraShake.instance.ShakeOffset : Vector3.zero;
        originalCamPos = mainCamera.transform.position - shakeOffset;
        originalCamSize = mainCamera.orthographicSize;

        if (playerTransform != null)
        {
            float facingDir = PlayerMovement.instance != null && PlayerMovement.instance.FacingLeft ? -1f : 1f;
            zoomedCamPos = new Vector3(playerTransform.position.x + Mathf.Abs(zoomedCamOffset.x) * facingDir,
                                       playerTransform.position.y + zoomedCamOffset.y, -10f);
        }

        PlayerAttack.instance?.SetPreparePose(true);
        trajectoryDrawer.SetDrawingEnabled(true);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionToDesign());
    }

    public void EndHoldBreath()
    {
        if (mainCamera == null) return;

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
        if (CameraShake.instance != null) CameraShake.instance.Shake(victory ? 0.2f : 0.5f, victory ? 0.15f : 0.3f);
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
            if (trajectoryDrawer != null) trajectoryDrawer.SetDrawingEnabled(false);

            yield return new WaitUntil(() => isBattleActive);
            isBattleActive = false;

            currentPhase = BattlePhase.Battle;
            isBattleRunning = true;
            if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = false;
            if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = false;
            yield return new WaitForSecondsRealtime(idleTime);

            while (!battleEnded) yield return null;
        }
    }

    IEnumerator TransitionToDesign()
    {
        isTransitioning = true;
        
        // [수정] 줌인이 진행되는 동안 카메라 추적 스크립트(CameraFollow)가 좌표를 방해하지 못하게 꺼버립니다.
        if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = false;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            if (mainCamera == null) yield break;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            mainCamera.orthographicSize = Mathf.Lerp(originalCamSize, zoomedCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(originalCamPos, zoomedCamPos, t);
            Time.timeScale = Mathf.Lerp(1f, slowMotionScale, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, darkOverlayAlpha, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = zoomedCamSize;
            mainCamera.transform.position = zoomedCamPos;
        }
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
            if (mainCamera == null) yield break;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration);
            mainCamera.orthographicSize = Mathf.Lerp(zoomedCamSize, originalCamSize, t);
            mainCamera.transform.position = Vector3.Lerp(zoomedCamPos, originalCamPos, t);
            Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(darkOverlayAlpha, 0f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = originalCamSize;
            mainCamera.transform.position = originalCamPos;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0f);
        
        // [수정] 줌아웃이 완전히 끝났을 때만, 배틀 상태를 체크하여 카메라 추적을 다시 켜줍니다.
        if (CameraFollow.instance != null && currentPhase != BattlePhase.Battle) 
            CameraFollow.instance.followEnabled = true;

        isTransitioning = false;
    }
}
