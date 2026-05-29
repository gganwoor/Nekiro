using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(1f, 0.5f);

    [HideInInspector] public bool followEnabled = true;

    private Vector3 followPos;
    private Vector3 lastAppliedShake = Vector3.zero;

    void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindTargetPlayer();
    }

    void Start()
    {
        if (target == null)
        {
            FindTargetPlayer();
        }
        else
        {
            followPos = TargetPos();
        }
    }

    void FindTargetPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player != null)
        {
            target = player.transform;
            followPos = TargetPos();
            Debug.Log("카메라가 타겟(Player)을 성공적으로 찾았습니다.");
        }
        else
        {
            Debug.LogWarning("카메라가 추적할 'Player' 태그를 가진 오브젝트를 찾지 못했습니다.");
        }
    }

    void LateUpdate()
    {
        Vector3 shake = CameraShake.instance != null ? CameraShake.instance.ShakeOffset : Vector3.zero;

        if (!followEnabled || target == null)
        {
            if (shake != Vector3.zero || lastAppliedShake != Vector3.zero)
            {
                transform.position = transform.position - lastAppliedShake + shake;
                lastAppliedShake = shake;
            }
            return;
        }

        lastAppliedShake = shake;
        followPos = Vector3.Lerp(followPos, TargetPos(), smoothSpeed * Time.deltaTime);
        transform.position = followPos + shake;
    }

    Vector3 TargetPos()
    {
        return new Vector3(target.position.x + offset.x,
                           target.position.y + offset.y,
                           transform.position.z);
    }
}
