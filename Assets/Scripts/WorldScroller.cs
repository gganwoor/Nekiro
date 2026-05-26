using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    public static WorldScroller instance;

    [Header("배경 타일 (왼쪽→오른쪽 순서)")]
    public SpriteRenderer[] bgTiles;
    public Sprite spriteA;
    public Sprite spriteB;
    public float bgTileWidth = 15f;

    [Header("바닥 타일 (왼쪽→오른쪽 순서)")]
    public SpriteRenderer[] floorTiles;
    public float floorTileWidth = 15f;

    private int[] bgSlots;
    public float WorldOffset { get; private set; }

    void Awake() { instance = this; }

    void Start()
    {
        bgSlots = new int[bgTiles.Length];
        for (int i = 0; i < bgTiles.Length; i++)
        {
            bgSlots[i] = i;
            UpdateBgSprite(i);
        }
    }

    public void Scroll(float delta)
    {
        WorldOffset += delta;

        MoveTiles(bgTiles, delta);
        MoveTiles(floorTiles, delta);

        Camera cam = Camera.main;
        float left  = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float right = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        RecycleBgTiles(left, right);
        RecycleFloorTiles(left, right);
    }

    void MoveTiles(SpriteRenderer[] tiles, float delta)
    {
        foreach (var t in tiles)
            t.transform.position -= new Vector3(delta, 0f, 0f);
    }

    // 배경: 슬롯 기반 스프라이트 전환 (a-b-b-b 패턴)
    void RecycleBgTiles(float left, float right)
    {
        for (int i = 0; i < bgTiles.Length; i++)
        {
            float x = bgTiles[i].transform.position.x;
            if (x + bgTileWidth < left)
            {
                bgTiles[i].transform.position = new Vector3(BgRightmostX() + bgTileWidth,
                    bgTiles[i].transform.position.y, bgTiles[i].transform.position.z);
                bgSlots[i] = BgMaxSlot() + 1;
                UpdateBgSprite(i);
            }
            else if (x - bgTileWidth > right)
            {
                bgTiles[i].transform.position = new Vector3(BgLeftmostX() - bgTileWidth,
                    bgTiles[i].transform.position.y, bgTiles[i].transform.position.z);
                bgSlots[i] = BgMinSlot() - 1;
                UpdateBgSprite(i);
            }
        }
    }

    // 바닥: 스프라이트 전환 없이 단순 재활용
    void RecycleFloorTiles(float left, float right)
    {
        foreach (var t in floorTiles)
        {
            float x = t.transform.position.x;
            if (x + floorTileWidth < left)
            {
                t.transform.position = new Vector3(FloorRightmostX() + floorTileWidth,
                    t.transform.position.y, t.transform.position.z);
            }
            else if (x - floorTileWidth > right)
            {
                t.transform.position = new Vector3(FloorLeftmostX() - floorTileWidth,
                    t.transform.position.y, t.transform.position.z);
            }
        }
    }

    void UpdateBgSprite(int i)
    {
        int seq = ((bgSlots[i] % 4) + 4) % 4;
        bgTiles[i].sprite = seq == 0 ? spriteA : spriteB;
    }

    float BgRightmostX()  { float m = float.MinValue; foreach (var t in bgTiles)    m = Mathf.Max(m, t.transform.position.x); return m; }
    float BgLeftmostX()   { float m = float.MaxValue; foreach (var t in bgTiles)    m = Mathf.Min(m, t.transform.position.x); return m; }
    float FloorRightmostX() { float m = float.MinValue; foreach (var t in floorTiles) m = Mathf.Max(m, t.transform.position.x); return m; }
    float FloorLeftmostX()  { float m = float.MaxValue; foreach (var t in floorTiles) m = Mathf.Min(m, t.transform.position.x); return m; }
    int BgMaxSlot() { int m = int.MinValue; foreach (int s in bgSlots) m = Mathf.Max(m, s); return m; }
    int BgMinSlot() { int m = int.MaxValue; foreach (int s in bgSlots) m = Mathf.Min(m, s); return m; }
}
