using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Areas (World, BoxCollider2D)")]
    [SerializeField] private BoxCollider2D spawnArea;   // 生成外框
    [SerializeField] private BoxCollider2D safeArea;    // 安全内框（不生成，可空）

    [Header("Prefab & Target")]
    [SerializeField] private Enemy     enemyPrefab;     // 由 LevelConfig 注入；也可手填
    [SerializeField] private Transform player;          // 目标（默认找 ViewerSystem 或 Tag=Player）

    // —— 窗口与计时（由 LevelRunner 配置）——
    float levelDuration;
    float startDelay;
    float stopBeforeEnd;
    float elapsed;
    bool  windowActive;

    // 刷怪间隔来自 LevelConfig（不再序列化在 Spawner 上）
    float spawnIntervalSec = 1.5f;
    float timer;

    bool warnedMissingPrefab = false;

    void Awake()
    {
        if (!player)
        {
            var vs = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
            if (vs) player = vs.transform;
            else
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged) player = tagged.transform;
            }
        }
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
            // 窗口外不刷怪，也不积累计时
            timer = 0.1f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (!EnsurePrefab())
        {
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

        SpawnOneAt(pos);            // 不改敌人速度，按预制体自身设置
        timer = Mathf.Max(0.05f, spawnIntervalSec);
    }

    /// <summary>
    /// LevelConfig 注入（用于设置 enemyPrefab 与 spawn 间隔）
    /// </summary>
    public void ApplyFromLevel(LevelConfig c)
    {
        if (!c) return;
        enemyPrefab     = c.enemyPrefab;
        spawnIntervalSec= Mathf.Max(0.05f, c.spawnInterval);
        warnedMissingPrefab = false;

        Debug.Log($"[Spawner] ApplyFromLevel: prefab={(enemyPrefab ? enemyPrefab.name : "<null>")}, interval={spawnIntervalSec:F2}s", this);
    }

    /// <summary>
    /// LevelRunner 配置刷怪“时间窗口”（开始延迟/结束前停止）。
    /// </summary>
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

    /// <summary>
    /// 停止并复位（LevelRunner 在关卡结束/中止时调用）
    /// </summary>
    public void StopAndReset()
    {
        windowActive = false;
        elapsed      = 0f;
        timer        = 0f;
    }

    /// <summary>
    /// 手动设置目标（若运行时切换玩家/代理对象）
    /// </summary>
    public void SetTarget(Transform t) => player = t;

    /// <summary>
    /// 手动生成一只（外部需要时可用）
    /// </summary>
    public Enemy SpawnOne()
    {
        if (!EnsurePrefab()) return null;
        if (!spawnArea) { Debug.LogWarning("[Spawner] spawnArea not assigned.", this); return null; }
        if (!TrySampleSpawnPosition(out var pos)) return null;
        return SpawnOneAt(pos);
    }

    /// <summary>
    /// 手动在指定位置生成一只
    /// </summary>
    public Enemy SpawnOneAt(Vector2 worldPos)
    {
        if (!EnsurePrefab()) return null;

        var e = Instantiate(enemyPrefab, worldPos, Quaternion.identity);

        if (!player)
        {
            var vs = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
            if (vs) player = vs.transform;
            else
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged) player = tagged.transform;
            }
        }
        if (player) e.SetTarget(player);

        // 不再设置移动速度：由 Enemy 预制体自身字段控制
        return e;
    }

    // —— 工具 —— //
    bool EnsurePrefab()
    {
        if (enemyPrefab) return true;
        if (!warnedMissingPrefab)
        {
            Debug.LogWarning("[Spawner] enemyPrefab is NULL. Assign via LevelConfig or Inspector.", this);
            warnedMissingPrefab = true;
        }
        return false;
    }

    bool TrySampleSpawnPosition(out Vector2 pos)
    {
        var sOk = TryGetRect(spawnArea, out Rect sRect);
        if (!sOk || sRect.width <= 0f || sRect.height <= 0f) { pos = default; return false; }

        Rect rRect = default;
        bool hasSafe = safeArea && TryGetRect(safeArea, out rRect);

        if (!hasSafe || rRect.width <= 0f || rRect.height <= 0f)
        {
            pos = SampleInRect(sRect);
            return true;
        }

        rRect = IntersectRect(sRect, rRect);
        if (rRect.width <= 0f || rRect.height <= 0f)
        {
            pos = SampleInRect(sRect);
            return true;
        }

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
            Gizmos.color = new Color(1f, 0f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
#endif
}
