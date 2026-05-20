using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    public enum TutorialStep
    {
        DrawTrajectory,
        ParryPractice,
        StaminaPractice,
        RealBattle,
        Clear
    }

    public TutorialStep currentStep = TutorialStep.DrawTrajectory;

    [Header("UI 연결")]
    public TextMeshProUGUI tutorialText;
    public GameObject tutorialPanel;

    [Header("연결")]
    public BattleManager battleManager;
    public TrajectoryDrawer trajectoryDrawer;
    public EnemyAttack enemyAttack;
    public EnemyStats enemyStats;

    private bool stepCleared = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        currentStep = TutorialStep.DrawTrajectory;
        ShowText("마우스를 클릭한 채로 드래그해\n궤적을 그려보세요.");
        trajectoryDrawer.SetDrawingEnabled(true);
        battleManager.enabled = false;

        yield return new WaitUntil(() => stepCleared);
        stepCleared = false;

        currentStep = TutorialStep.ParryPractice;
        ShowText("적의 붉은 예고선이 표시됩니다.\n예고선을 가로지르도록 궤적을 그려\n공격을 막아보세요.");
        enemyAttack.StartAttack();
        trajectoryDrawer.SetDrawingEnabled(true);

        yield return new WaitUntil(() => stepCleared);
        stepCleared = false;

        currentStep = TutorialStep.StaminaPractice;
        ShowText("패링에 성공하면 적의 스태미나가 감소합니다.\n적의 스태미나를 모두 소진시켜보세요.");
        Coroutine staminaLoop = StartCoroutine(StaminaAttackLoop());

        yield return new WaitUntil(() => stepCleared);
        stepCleared = false;
        StopCoroutine(staminaLoop);
        trajectoryDrawer.SetDrawingEnabled(false);

        currentStep = TutorialStep.RealBattle;
        ShowText("이제 직접 싸워보세요.");
        battleManager.enabled = true;

        yield return new WaitUntil(() => stepCleared);
        stepCleared = false;

        currentStep = TutorialStep.Clear;
        ShowText("클리어!");
        yield return new WaitForSeconds(2f);
        HideText();
        SceneManager.LoadScene("TitleScene");
    }

    IEnumerator StaminaAttackLoop()
    {
        while (true)
        {
            enemyAttack.StartAttack();
            trajectoryDrawer.SetDrawingEnabled(true);
            yield return new WaitForSeconds(enemyAttack.warningDuration);
            trajectoryDrawer.SetDrawingEnabled(false);
            yield return new WaitForSeconds(1f);
        }
    }

    public void ShowText(string message)
    {
        tutorialPanel.SetActive(true);
        tutorialText.text = message;
    }

    public void HideText()
    {
        tutorialPanel.SetActive(false);
    }

    public void CompleteStep()
    {
        stepCleared = true;
    }
}
