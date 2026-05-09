using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    public enum BattlePhase
    {
        Wait,
        Design,
        Execute,
        Result
    }

    public BattlePhase currentPhase = BattlePhase.Wait;
    public float designPhaseTime = 3f;
    public float executePhaseTime = 1f;
    public float resultPhaseTime = 1f;

    private EnemyAttack enemyAttack;
    private TrajectoryDrawer trajectoryDrawer;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        enemyAttack = FindObjectOfType<EnemyAttack>();
        trajectoryDrawer = FindObjectOfType<TrajectoryDrawer>();

        StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        while (true)
        {
            yield return StartCoroutine(WaitPhase());
            yield return StartCoroutine(DesignPhase());
            yield return StartCoroutine(ExecutePhase());
            yield return StartCoroutine(ResultPhase());
        }
    }

    IEnumerator WaitPhase()
    {
        currentPhase = BattlePhase.Wait;
        Debug.Log("대기 페이즈");
        yield return new WaitForSeconds(1f);
        enemyAttack.StartAttack();
    }

    IEnumerator DesignPhase()
    {
        currentPhase = BattlePhase.Design;
        Debug.Log("설계 페이즈");
        trajectoryDrawer.SetDrawingEnabled(true);
        yield return new WaitForSeconds(designPhaseTime);
        trajectoryDrawer.SetDrawingEnabled(false);
    }

    IEnumerator ExecutePhase()
    {
        currentPhase = BattlePhase.Execute;
        Debug.Log("실행 페이즈");
        trajectoryDrawer.ExecuteJudgement();
        yield return new WaitForSeconds(executePhaseTime);
    }

    IEnumerator ResultPhase()
    {
        currentPhase = BattlePhase.Result;
        Debug.Log("결과 페이즈");
        yield return new WaitForSeconds(resultPhaseTime);
    }
}