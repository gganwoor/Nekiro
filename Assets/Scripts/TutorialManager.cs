using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;
    public static bool inputBlocked = false;

    public enum TutorialStep
    {
        Welcome,
        Movement,
        AttackPractice,
        DrawTrajectory,
        Parry,
        Counter,
        Dodge,
        RealBattle,
        Clear
    }

    public TutorialStep currentStep = TutorialStep.Welcome;

    [Header("UI 연결")]
    public TextMeshProUGUI tutorialText;
    public GameObject tutorialPanel;

    [Header("적 등장")]
    public GameObject enemyVisualRoot;

    [Header("이동 튜토리얼")]
    public float movementTargetX = 8f;

    [Header("공격 연습 (허수아비)")]
    public GameObject scarecrowPrefab;
    public float scarecrowSpawnOffset = 2.5f;
    public int scarecrowHitsRequired = 3;

    [Header("디버그")]
    public bool debugSkipToRealBattle = false;

    [HideInInspector] public GameObject checkboxPanel;
    [HideInInspector] public Toggle moveToggle;
    [HideInInspector] public Toggle attackToggle;

    private TrajectoryDrawer trajectoryDrawer;
    private EnemyStats enemyStats;
    private bool stepCleared = false;

    void Awake()
    {
        instance = this;
        inputBlocked = false;
    }

    void Start()
    {
        trajectoryDrawer = FindFirstObjectByType<TrajectoryDrawer>();
        enemyStats = FindFirstObjectByType<EnemyStats>();
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        if (BattleManager.instance != null) BattleManager.instance.autoPhaseEnabled = false;

        if (debugSkipToRealBattle)
        {
            if (enemyVisualRoot != null)
            {
                enemyVisualRoot.SetActive(true);
                EnemyAI.instance?.GetComponentInChildren<Animator>(true)?.Play("Idle", 0, 0f);
            }
            if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = false;
            if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = false;
            enemyStats?.ResetStats();
            currentStep = TutorialStep.RealBattle;
            HideText();
            yield return RunBattlesUntilCleared();
            stepCleared = false;
            currentStep = TutorialStep.Clear;
            ShowText("클리어!");
            yield return new WaitForSeconds(2f);
            HideText();
            SceneManager.LoadScene("TitleScene");
            yield break;
        }

        if (enemyVisualRoot != null) enemyVisualRoot.SetActive(false);

        // 0. 환영
        currentStep = TutorialStep.Welcome;
        inputBlocked = true;
        ShowText("튜토리얼에 오신 것을 환영합니다.\nSpace 또는 Enter로 계속");
        yield return null;
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
            yield return null;
        inputBlocked = false;
        yield return new WaitForSeconds(0.3f);

        // 1. 이동 튜토리얼 — travel mode
        currentStep = TutorialStep.Movement;
        if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = true;
        if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = true;

        Debug.Log($"[Tutorial] Movement 시작 — WorldScroller: {(WorldScroller.instance != null ? "OK" : "NULL")}");

        float playerX = PlayerMovement.instance != null ? PlayerMovement.instance.transform.position.x : 0f;
        float initialOffset = WorldScroller.instance != null ? WorldScroller.instance.WorldOffset : 0f;
        GameObject marker = CreateMovementMarker(playerX + movementTargetX);
        ShowText("A, D 키로 이동하여\n표시된 위치까지 걸어가세요.");

        while (true)
        {
            float traveled = WorldScroller.instance != null
                ? WorldScroller.instance.WorldOffset - initialOffset : 0f;
            if (traveled >= movementTargetX) break;
            if (marker != null)
                marker.transform.position = new Vector3(playerX + movementTargetX - traveled, 0f, 0f);
            yield return null;
        }
        Destroy(marker);
        Debug.Log("[Tutorial] Movement 완료");
        yield return new WaitForSeconds(0.4f);

        // 2. 공격 연습 — battle mode 전환
        currentStep = TutorialStep.AttackPractice;
        if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = false;
        if (CameraFollow.instance != null) CameraFollow.instance.followEnabled = false;
        GameObject scarecrowObj = SpawnScarecrow();
        UpdateScarecrowProgress(0, scarecrowHitsRequired);

        while (!stepCleared)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                PlayerAttack.instance?.TriggerAttack();
            yield return null;
        }
        stepCleared = false;
        if (scarecrowObj != null) Destroy(scarecrowObj);
        yield return new WaitForSeconds(0.5f);

        // 3. 숨 참기 + 궤적 그리기 — 실제 적 등장
        currentStep = TutorialStep.DrawTrajectory;
        if (enemyVisualRoot != null)
        {
            enemyVisualRoot.SetActive(true);
            EnemyAI.instance?.GetComponentInChildren<Animator>(true)?.Play("Idle", 0, 0f);
        }
        enemyStats?.ResetStats();
        ResetEnemyComponents();
        ShowText("우클릭으로 숨을 참으면\n적의 다음 움직임이 미리 보입니다.\n마우스로 궤적을 그어 막아보세요.");
        if (!BattleManager.instance.IsBattleRunning)
            BattleManager.instance.StartBattle();

        bool heldBreathOnce = false;
        trajectoryDrawer.ResetJudgement();
        while (true)
        {
            if (BattleManager.instance.IsHoldingBreath) heldBreathOnce = true;
            if (heldBreathOnce && trajectoryDrawer.isReadyToJudge) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);

        // 4. 패링 연습
        currentStep = TutorialStep.Parry;
        enemyStats?.ResetStats();
        ResetEnemyComponents();
        ShowText("예고선을 가로지르면 패링 성공!\n한 번 패링해보세요.");
        yield return RunBattlesUntilCleared();
        stepCleared = false;
        yield return new WaitForSeconds(0.5f);

        // 5. 카운터 연습
        currentStep = TutorialStep.Counter;
        enemyStats?.ResetStats();
        ResetEnemyComponents();
        ShowText("패링 성공 후 Space로 카운터 반격하세요!");
        yield return RunBattlesUntilCleared();
        stepCleared = false;
        yield return new WaitForSeconds(0.5f);

        // 6. 회피 연습
        currentStep = TutorialStep.Dodge;
        enemyStats?.ResetStats();
        ResetEnemyComponents();
        ShowText("Left Shift로 적의 공격을 회피할 수 있어요!\n적이 공격할 때 Left Shift로 회피해보세요.");
        yield return RunBattlesUntilCleared();
        stepCleared = false;
        yield return new WaitForSeconds(0.5f);

        // 7. 실전 전투
        currentStep = TutorialStep.RealBattle;
        enemyStats?.ResetStats();
        ResetEnemyComponents();
        ShowText("이제 직접 싸워보세요!");
        yield return RunBattlesUntilCleared();
        stepCleared = false;

        // 클리어
        currentStep = TutorialStep.Clear;
        ShowText("클리어!");
        yield return new WaitForSeconds(2f);
        HideText();
        SceneManager.LoadScene("TitleScene");
    }

    void ResetEnemyComponents()
    {
        if (EnemyAI.instance != null)
            EnemyAI.instance.enabled = true;

        EnemyAttack attack = EnemyAI.instance?.GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.enabled = true;
            attack.InterruptAttack();
        }
    }

    IEnumerator RunBattlesUntilCleared()
    {
        if (!BattleManager.instance.IsBattleRunning)
            BattleManager.instance.StartBattle();

        while (!stepCleared)
        {
            BattleManager.instance.battleCycleComplete = false;
            yield return new WaitUntil(() => stepCleared || BattleManager.instance.battleCycleComplete);
        }
    }

    GameObject CreateMovementMarker(float targetX)
    {
        GameObject go = new GameObject("MovementMarker");
        go.transform.position = new Vector3(targetX, 0f, 0f);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.9f, 0.1f, 0.9f);
        lr.endColor = new Color(1f, 0.9f, 0.1f, 0.9f);
        lr.startWidth = 0.07f;
        lr.endWidth = 0.07f;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(0f, -2.5f, 0f));
        lr.SetPosition(1, new Vector3(0f,  2.5f, 0f));
        lr.sortingOrder = 10;
        return go;
    }

    GameObject SpawnDecoScarecrow(Vector3 pos)
    {
        if (scarecrowPrefab != null)
        {
            GameObject obj = Instantiate(scarecrowPrefab, pos, Quaternion.identity);
            TutorialScarecrow ts = obj.GetComponent<TutorialScarecrow>();
            if (ts != null) Destroy(ts);
            return obj;
        }

        GameObject go = new GameObject("DecoScarecrow");
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.color = new Color(0.7f, 0.5f, 0.25f);
        sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(1.2f, 2.5f, 1f);

        return go;
    }

    GameObject SpawnScarecrow()
    {
        float px = PlayerMovement.instance != null ? PlayerMovement.instance.transform.position.x : 0f;
        float py = PlayerMovement.instance != null ? PlayerMovement.instance.transform.position.y : 0f;
        Vector3 pos = new Vector3(px + scarecrowSpawnOffset, py, 0f);

        if (scarecrowPrefab != null)
        {
            GameObject obj = Instantiate(scarecrowPrefab, pos, Quaternion.identity);
            TutorialScarecrow ts = obj.GetComponent<TutorialScarecrow>();
            if (ts != null) ts.hitsRequired = scarecrowHitsRequired;
            return obj;
        }

        GameObject go = new GameObject("Scarecrow");
        go.transform.position = pos;
        go.layer = LayerMask.NameToLayer("Enemy");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.color = new Color(0.85f, 0.6f, 0.3f);
        sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(1.2f, 2.5f, 1f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        TutorialScarecrow ts2 = go.AddComponent<TutorialScarecrow>();
        ts2.hitsRequired = scarecrowHitsRequired;
        return go;
    }

    public void UpdateScarecrowProgress(int current, int required)
    {
        ShowText($"스페이스바로 허수아비를 공격하세요!\n({current} / {required})");
    }

    public void ShowText(string message)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null) tutorialText.text = message;
    }

    public void HideText()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void CompleteStep()
    {
        stepCleared = true;
    }
}
