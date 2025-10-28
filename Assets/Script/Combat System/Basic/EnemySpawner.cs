using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Areas (World, BoxCollider2D)")]
    [SerializeField] private BoxCollider2D spawnArea;  // 生成框（外框）
    [SerializeField] private BoxCollider2D safeArea;   // 安全框（内框，不生成）

    [Header("Spawn Params")]
    [SerializeField] private Enemy enemyPrefab;        // 将由 LevelRunner/LevelConfig 注入；也可手填
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float moveSpeed  = 1.8f;
    [SerializeField] private Transform player;

    // 窗口控制
    float levelDuration;
    float startDelay;
    float stopBeforeEnd;
    float elapsed;
    bool  windowActive;

    float timer;
    bool  warnedMissingPrefab = false;

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        timer = 0.1f;
        elapsed = 0f;
    }

    void Update()
    {
        if (!windowActive) return;

        elapsed += Time.deltaTime;

        bool inWindow = elapsed >= startDelay &&
                        elapsed <= Mathf.Max(0f, levelDuration - stopBeforeEnd);

        if (!inWindow)
        {
            timer = 0.1f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (!enemyPrefab)
        {
            if (!warnedMissingPrefab)
            {
                Debug.LogWarning("[Spawner] enemyPrefab is NULL. Assign in LevelConfig or Inspector.", this);
                warnedMissingPrefab = true;
            }
            timer = 0.25f;
            return;
        }

        if (!spawnArea)
        {
            Debug.LogWarning("[Spawner] spawnArea not assigned.", this);
            timer = 0.5f;
            return;
        }

        if (!TrySampleSpawnPosition(out var pos))
        {
            timer = 0.5f;
            return;
        }

        var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        e.SetTarget(player);
        e.SetMoveSpeed(moveSpeed);

        timer = spawnInterval;
    }

    bool TrySampleSpawnPosition(out Vector2 pos)
    {
        var sOk = TryGetRect(spawnArea, out Rect sRect);
        if (!sOk || sRect.width <= 0f || sRect.height <= 0f) { pos = default; return false; }

        Rect rRect = default;
        bool hasSafe = safeArea && TryGetRect(safeArea, out rRect);

        if (!hasSafe || rRect.width <= 0f || rRect.height <= 0f) { pos = SampleInRect(sRect); return true; }

        rRect = IntersectRect(sRect, rRect);
        if (rRect.width <= 0f || rRect.height <= 0f) { pos = SampleInRect(sRect); return true; }

        float topH   = Mathf.Max(0f, (sRect.yMax - rRect.yMax));
        float botH   = Mathf.Max(0f, (rRect.yMin - sRect.yMin));
        float sideH  = Mathf.Max(0f, rRect.height);
        float leftW  = Mathf.Max(0f, (rRect.xMin - sRect.xMin));
        float rightW = Mathf.Max(0f, (sRect.xMax - rRect.xMax));

        float areaTop   = sRect.width * topH;
        float areaBot   = sRect.width * botH;
        float areaLeft  = leftW * sideH;
        float areaRight = rightW * sideH;

        float total = areaTop + areaBot + areaLeft + areaRight;
        if (total <= 0f) { pos = default; return false; }

        float pick = Random.value * total;
        if (pick < areaTop)
        {
            pos = new Vector2(Random.Range(sRect.xMin, sRect.xMax), Random.Range(rRect.yMax, sRect.yMax));
            return true;
        }
        pick -= areaTop;

        if (pick < areaBot)
        {
            pos = new Vector2(Random.Range(sRect.xMin, sRect.xMax), Random.Range(sRect.yMin, rRect.yMin));
            return true;
        }
        pick -= areaBot;

        if (pick < areaLeft)
        {
            pos = new Vector2(Random.Range(sRect.xMin, rRect.xMin), Random.Range(rRect.yMin, rRect.yMax));
            return true;
        }

        pos = new Vector2(Random.Range(rRect.xMax, sRect.xMax), Random.Range(rRect.yMin, rRect.yMax));
        return true;
    }

    static bool TryGetRect(BoxCollider2D col, out Rect rect)
    {
        var b = col.bounds;
        rect = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        return true;
    }

    static Rect IntersectRect(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin) return new Rect(0, 0, 0, 0);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    static Vector2 SampleInRect(Rect r)
    {
        return new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
    }

    // LevelConfig 注入
    public void ApplyFromLevel(LevelConfig c)
    {
        if (!c) return;
        enemyPrefab   = c.enemyPrefab;
        spawnInterval = Mathf.Max(0.05f, c.spawnInterval);
        warnedMissingPrefab = false;

        Debug.Log($"[Spawner] ApplyFromLevel: prefab={(enemyPrefab? enemyPrefab.name : "<null>")}, interval={spawnInterval}", this);
    }

    public void ConfigureWindow(float levelDur, float startDelaySec, float stopEarlySec)
    {
        levelDuration = Mathf.Max(0f, levelDur);
        startDelay    = Mathf.Max(0f, startDelaySec);
        stopBeforeEnd = Mathf.Max(0f, stopEarlySec);

        elapsed      = 0f;
        timer        = 0.1f;
        windowActive = true;

        Debug.Log($"[Spawner] Window configured: startDelay={startDelay}s, stopEarly={stopBeforeEnd}s, levelDur={levelDuration}s", this);
    }

    public void StopAndReset()
    {
        windowActive = false;
        elapsed = 0f;
        timer = 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (spawnArea)
        {
            var b = spawnArea.bounds;
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
        if (safeArea)
        {
            var b = safeArea.bounds;
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(1f, 0f, 0f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
#endif
}
