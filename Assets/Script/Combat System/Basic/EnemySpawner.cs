using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Areas (World, BoxCollider2D)")]
    [SerializeField] private BoxCollider2D spawnArea;  // 生成框（外框）
    [SerializeField] private BoxCollider2D safeArea;   // 安全框（内框）

    [Header("Spawn Params")]
    [SerializeField] private Enemy enemyPrefab;        // 可由 LevelRunner 注入
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float moveSpeed  = 1.8f;
    [SerializeField] private Transform player;

    // —— 刷怪窗口控制 —— 
    private float levelDuration;
    private float startDelay;
    private float stopBeforeEnd;
    private float elapsed;
    private bool  windowActive;

    private float timer;
    private bool warnedMissingPrefab = false;

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
                Debug.LogWarning("[Spawner] enemyPrefab is NULL. Waiting for LevelRunner injection…", this);
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

        Vector2 pos;
        if (!TrySampleSpawnPosition(out pos))
        {
            // 配置异常（比如 safeArea 几乎占满 spawnArea），暂缓重试
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
        if (!sOk || sRect.width <= 0f || sRect.height <= 0f)
        {
            pos = default;
            return false;
        }

        Rect rRect = default;
        bool hasSafe = safeArea && TryGetRect(safeArea, out rRect);

        // 若没有安全框，直接在生成框内采样
        if (!hasSafe || rRect.width <= 0f || rRect.height <= 0f)
        {
            pos = SampleInRect(sRect);
            return true;
        }

        // 确保安全框在生成框内，若超界取交集
        rRect = IntersectRect(sRect, rRect);
        if (rRect.width <= 0f || rRect.height <= 0f)
        {
            // 交集为空 => 等同于没有安全框
            pos = SampleInRect(sRect);
            return true;
        }

        // 计算四个条带（spawn - safe）的面积与权重
        // 上下条：宽 = sRect.width，高 = 上/下剩余
        float topH    = Mathf.Max(0f, (sRect.yMax - rRect.yMax));
        float botH    = Mathf.Max(0f, (rRect.yMin - sRect.yMin));
        float sideH   = Mathf.Max(0f, rRect.height);
        float leftW   = Mathf.Max(0f, (rRect.xMin - sRect.xMin));
        float rightW  = Mathf.Max(0f, (sRect.xMax - rRect.xMax));

        float areaTop    = sRect.width * topH;
        float areaBot    = sRect.width * botH;
        float areaLeft   = leftW * sideH;
        float areaRight  = rightW * sideH;

        float totalArea = areaTop + areaBot + areaLeft + areaRight;
        if (totalArea <= 0f)
        {
            // 安全框≈生成框，几乎没有可用区域
            pos = default;
            return false;
        }

        float pick = Random.value * totalArea;
        if (pick < areaTop)
        {
            // 顶条：x ∈ [s.xMin, s.xMax], y ∈ [r.yMax, s.yMax]
            float x = Random.Range(sRect.xMin, sRect.xMax);
            float y = Random.Range(rRect.yMax, sRect.yMax);
            pos = new Vector2(x, y);
            return true;
        }
        pick -= areaTop;

        if (pick < areaBot)
        {
            // 底条：x ∈ [s.xMin, s.xMax], y ∈ [s.yMin, r.yMin]
            float x = Random.Range(sRect.xMin, sRect.xMax);
            float y = Random.Range(sRect.yMin, rRect.yMin);
            pos = new Vector2(x, y);
            return true;
        }
        pick -= areaBot;

        if (pick < areaLeft)
        {
            // 左条：x ∈ [s.xMin, r.xMin], y ∈ [r.yMin, r.yMax]
            float x = Random.Range(sRect.xMin, rRect.xMin);
            float y = Random.Range(rRect.yMin, rRect.yMax);
            pos = new Vector2(x, y);
            return true;
        }
        // 右条：x ∈ [r.xMax, s.xMax], y ∈ [r.yMin, r.yMax]
        float rx = Random.Range(rRect.xMax, sRect.xMax);
        float ry = Random.Range(rRect.yMin, rRect.yMax);
        pos = new Vector2(rx, ry);
        return true;
    }

    static bool TryGetRect(BoxCollider2D col, out Rect rect)
    {
        // 使用 world AABB，Z 忽略
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
        float x = Random.Range(r.xMin, r.xMax);
        float y = Random.Range(r.yMin, r.yMax);
        return new Vector2(x, y);
    }

    // —— LevelRunner 注入基础参数 —— 
    public void SetEnemyPrefab(Enemy e)
    {
        enemyPrefab = e;
        warnedMissingPrefab = false;
    }

    public void SetSpawnInterval(float s)
    {
        spawnInterval = Mathf.Max(0.05f, s);
    }

    public void ApplyFromLevel(LevelConfig c)
    {
        if (!c) return;
        SetEnemyPrefab(c.enemyPrefab);
        SetSpawnInterval(c.spawnInterval);
        string prefabName = (enemyPrefab != null) ? enemyPrefab.name : "<null>";
        Debug.Log($"[Spawner] ApplyFromLevel: prefab={prefabName}, interval={spawnInterval}", this);
    }

    // —— 刷怪窗口：LevelRunner 在每关开始时调用 —— 
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

    // —— 被 LevelRunner 在关卡切换时调用，重置状态 —— 
    public void StopAndReset()
    {
        windowActive = false;
        elapsed = 0f;
        timer = 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 可视化两个框
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
