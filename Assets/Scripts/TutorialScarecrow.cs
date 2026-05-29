using System.Collections;
using UnityEngine;

public class TutorialScarecrow : MonoBehaviour
{
    public static TutorialScarecrow instance;
    public int hitsRequired = 3;
    private int hitCount = 0;
    private SpriteRenderer sr;

    void Awake()
    {
        instance = this;
        sr = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void OnHit()
    {
        hitCount++;
        TutorialManager.instance?.UpdateScarecrowProgress(hitCount, hitsRequired);
        StartCoroutine(FlashRoutine());
        if (hitCount >= hitsRequired)
            TutorialManager.instance?.CompleteStep();
    }

    IEnumerator FlashRoutine()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = Color.white;
        yield return new WaitForSecondsRealtime(0.08f);
        if (sr != null) sr.color = orig;
    }
}
