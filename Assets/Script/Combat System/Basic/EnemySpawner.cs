using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Params")]
    [SerializeField] private Enemy enemyPrefab;         // 可留空：由 LevelRunner 注入
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float radiusMin = 8f;
    [SerializeField] private float radiusMax = 10f;
    [SerializeField] private float moveSpeed  = 1.8f;
    [SerializeField] private Transform player;

    // —— 刷怪窗口控制 —— 
    private float levelDuration;   // 本关总时长（秒）
    private float startDelay;      // 开场延迟多少秒后开始刷怪
    private float stopBeforeEnd;   // 结束前提前多少秒停止刷怪
    private float elapsed;         // 本关已过时间
    private bool  windowActive;    // 是否启用窗口逻辑

    private float timer;           // 刷怪计时器
    private bool warnedMissingPrefab = false;

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        timer = 0.1f;     // 让第一只尽快尝试
        elapsed = 0f;
    }

    void Update()
    {
        if (!windowActive) return;

        elapsed += Time.deltaTime;

        // 是否处于允许刷怪的时间窗口
        bool inWindow = elapsed >= startDelay &&
                        elapsed <= Mathf.Max(0f, levelDuration - stopBeforeEnd);

        if (!inWindow)
        {
            // 窗口未到或已过：短暂重试等待窗口到来/结束
            timer = 0.1f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (!enemyPrefab)
        {
            if (!warnedMissingPrefab)
            {
                Debug.LogWarning("[Spawner] enemyPrefab is NULL. Waiting for LevelRunner injection…");
                warnedMissingPrefab = true;
            }
            timer = 0.25f;
            return;
        }

        SpawnOne();
        timer = spawnInterval;
    }

    void SpawnOne()
    {
        float r   = Random.Range(radiusMin, radiusMax);
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;

        var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        e.SetTarget(player);
        e.SetMoveSpeed(moveSpeed);
    }

    // —— LevelRunner 注入基础参数 —— 
    public void SetEnemyPrefab(Enemy e)
    {
        enemyPrefab = e;
        warnedMissingPrefab = false; // 一旦注入成功，重置告警
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
        Debug.Log($"[Spawner] ApplyFromLevel: prefab={prefabName}, interval={spawnInterval}");
    }

    // —— 刷怪窗口：LevelRunner 在每关开始时调用 —— 
    public void ConfigureWindow(float levelDur, float startDelaySec, float stopEarlySec)
    {
        levelDuration = Mathf.Max(0f, levelDur);
        startDelay    = Mathf.Max(0f, startDelaySec);
        stopBeforeEnd = Mathf.Max(0f, stopEarlySec);

        elapsed      = 0f;
        timer        = 0.1f;   // 进入窗口后尽快刷第一只
        windowActive = true;

        Debug.Log($"[Spawner] Window configured: startDelay={startDelay}s, stopEarly={stopBeforeEnd}s, levelDur={levelDuration}s");
    }

    // —— 被 LevelRunner 在关卡切换时调用，重置状态 —— 
    public void StopAndReset()
    {
        windowActive = false;
        elapsed = 0f;
        timer = 0f;
    }
}
