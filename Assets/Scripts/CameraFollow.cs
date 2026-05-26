using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(1f, 0.5f);

    [HideInInspector] public bool followEnabled = true;

    private Vector3 followPos;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (target != null)
            followPos = TargetPos();
    }

    void LateUpdate()
    {
        if (!followEnabled || target == null) return;

        followPos = Vector3.Lerp(followPos, TargetPos(), smoothSpeed * Time.deltaTime);

        Vector3 shake = CameraShake.instance != null ? CameraShake.instance.ShakeOffset : Vector3.zero;
        transform.position = followPos + shake;
    }

    Vector3 TargetPos()
    {
        return new Vector3(target.position.x + offset.x,
                           target.position.y + offset.y,
                           transform.position.z);
    }
}
