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
        BasicControls,
        DrawTrajectory,
        ParryPractice,
        StaminaPractice,
        RealBattle,
        Clear
    }

    public TutorialStep currentStep = TutorialStep.Welcome;

    [Header("UI 연결")]
    public TextMeshProUGUI tutorialText;
    public GameObject tutorialPanel;
    public Vector2 panelPadding = new Vector2(40f, 30f);

    [Header("체크박스 UI")]
    public GameObject checkboxPanel;
    public Toggle moveToggle;
    public Toggle attackToggle;

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
        trajectoryDrawer = FindObjectOfType<TrajectoryDrawer>();
        enemyStats = FindObjectOfType<EnemyStats>();
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        // 0단계: 환영
        currentStep = TutorialStep.Welcome;
        inputBlocked = true;
        ShowText("튜토리얼에 오신 것을 환영합니다.");
        yield return null; // 같은 프레임 Space 입력 무시
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
            yield return null;
        inputBlocked = false;

        yield return new WaitForSeconds(0.3f);

        // 1단계: 기본 조작 (이동 + 공격)
        currentStep = TutorialStep.BasicControls;
        if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = true;
        ShowText("A, D로 이동 / Space 키로 공격");
        ShowCheckboxes(false, false);

        bool movedDone = false, attackDone = false;
        while (!movedDone || !attackDone)
        {
            if (!movedDone && Input.GetAxisRaw("Horizontal") != 0)
            {
                movedDone = true;
                ShowCheckboxes(true, attackDone);
            }
            if (!attackDone && Input.GetKeyDown(KeyCode.Space))
            {
                PlayerAttack.instance?.TriggerAttack();
                attackDone = true;
                ShowCheckboxes(movedDone, true);
            }
            yield return null;
        }

        if (PlayerMovement.instance != null) PlayerMovement.instance.isTravelMode = false;
        HideCheckboxes();
        yield return new WaitForSeconds(0.5f);

        // 2단계: 숨 참기 + 궤적 그리기
        currentStep = TutorialStep.DrawTrajectory;
        ShowText("우클릭을 눌러 숨 참기에 돌입하면\n적의 공격 예고선이 보입니다.\n마우스를 드래그해 궤적을 그려보세요.");
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

        // 3단계: 패링 연습
        currentStep = TutorialStep.ParryPractice;
        ShowText("예고선을 가로지르면 패링 성공!\n한 번 패링해보세요.");

        yield return RunBattlesUntilCleared();
        stepCleared = false;

        yield return new WaitForSeconds(0.5f);

        // 4단계: 스태미나 소진
        currentStep = TutorialStep.StaminaPractice;
        enemyStats?.ResetStats();
        ShowText("패링 성공 시 적의 스태미나가 감소합니다.\n스태미나를 모두 소진시켜 보세요.");

        yield return RunBattlesUntilCleared();
        stepCleared = false;

        yield return new WaitForSeconds(0.5f);

        // 5단계: 실전 전투
        currentStep = TutorialStep.RealBattle;
        enemyStats?.ResetStats();
        ShowText("이제 직접 싸워보세요.");

        yield return RunBattlesUntilCleared();
        stepCleared = false;

        // 클리어
        currentStep = TutorialStep.Clear;
        ShowText("클리어!");
        yield return new WaitForSeconds(2f);
        HideText();
        SceneManager.LoadScene("TitleScene");
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

    void ShowCheckboxes(bool moveDone, bool attackDone)
    {
        if (checkboxPanel != null) checkboxPanel.SetActive(true);
        if (moveToggle != null) moveToggle.isOn = moveDone;
        if (attackToggle != null) attackToggle.isOn = attackDone;
    }

    void HideCheckboxes()
    {
        if (checkboxPanel != null) checkboxPanel.SetActive(false);
    }

    public void ShowText(string message)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null)
        {
            tutorialText.text = message;
        }
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
