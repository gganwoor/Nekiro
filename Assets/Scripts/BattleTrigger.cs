using UnityEngine;

public class BattleTrigger : MonoBehaviour
{
    [System.Serializable]
    public struct TriggerZone
    {
        public string label;
        public float worldOffset;
        [HideInInspector] public bool triggered;
    }

    public TriggerZone[] zones;

    void Update()
    {
        if (WorldScroller.instance == null || BattleManager.instance == null) return;

        float offset = WorldScroller.instance.WorldOffset;

        for (int i = 0; i < zones.Length; i++)
        {
            if (!zones[i].triggered && offset >= zones[i].worldOffset)
            {
                zones[i].triggered = true;
                BattleManager.instance.StartBattle();
                break; // 한 번에 하나씩만
            }
        }
    }
}
