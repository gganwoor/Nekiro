using System.Collections.Generic;
using UnityEngine;

public class JudgementSystem : MonoBehaviour
{
    public static bool IsIntersecting(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector2 p1 = a1, p2 = a2, p3 = b1, p4 = b2;

        float d1 = Direction(p3, p4, p1);
        float d2 = Direction(p3, p4, p2);
        float d3 = Direction(p1, p2, p3);
        float d4 = Direction(p1, p2, p4);

        if(((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        if(d1 == 0 && OnSegment(p3, p4, p1)) return true;
        if(d2 == 0 && OnSegment(p3, p4, p2)) return true;
        if(d3 == 0 && OnSegment(p1, p2, p3)) return true;
        if(d4 == 0 && OnSegment(p1, p2, p4)) return true;

        return false;
    }

    static float Direction(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
    }

    static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Min(a.x, b.x) <= c.x && c.x <= Mathf.Max(a.x, b.x) && Mathf.Min(a.y, b.y) <= c.y && c.y <= Mathf.Max(a.y, b.y);
    }

    public static bool CheckParry(List<Vector3> playerPoints, Vector3 enemyStart, Vector3 enemyEnd)
    {
        for(int i = 0; i < playerPoints.Count - 1; i++)
        {
            if(IsIntersecting(playerPoints[i], playerPoints[i + 1], enemyStart, enemyEnd))
                return true;
        }
        return false;
    }
}