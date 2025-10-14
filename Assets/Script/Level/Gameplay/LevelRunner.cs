using System.Collections;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [SerializeField] private RhythmSystem  rhythm;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;
    [SerializeField] private AudioSource   music;   // 仅用于播放BGM；关卡结束仍按 levelDurationSeconds

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;
    public LevelConfig Current { get; private set; }

    Coroutine timerCo;

    public void Apply(LevelConfig c)
    {
        // —— 跨关前：先彻底清场 —— 
        CleanLevelState();

        Current = c;
        if (!Current)
        {
            Debug.LogError("[LevelRunner] LevelConfig is null");
            return;
        }

        // —— 播放（可选）：有 bgm 就播，但不依赖它计时 —— 
        if (music)
        {
            music.Stop();
            music.clip         = Current.bgm;
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f; // 2D 声音
            if (music.clip) music.Play();
        }

        // —— 节奏参数 —— 
        float spb = 60f / Mathf.Max(1f, Current.bpm);
        rhythm.SetCycleSeconds(Mathf.Max(0.01f, Current.cycleBeats * spb));
        rhythm.hitCenter    = Current.hitCenter;
        rhythm.hitHalfWidth = Current.hitHalfWidth;

        // —— 指令长度 —— 
        pattern.SetSequenceLength(Current.sequenceLength);

        // —— 刷怪参数 + 窗口 —— 
        if (!spawner) spawner = FindObjectOfType<EnemySpawner>(true);
        if (spawner)
        {
            spawner.ApplyFromLevel(Current);
            spawner.ConfigureWindow(Current.levelDurationSeconds,
                                    Current.spawnStartDelay,
                                    Current.spawnStopEarly);
        }
        else
        {
            Debug.LogError("[LevelRunner] EnemySpawner not found in scene!");
        }

        // —— 触发新的回合：让 PatternSystem 重建新指令串/格子 —— 
        rhythm.ForceNextRound();

        OnLevelApplied?.Invoke();

        // —— 只按关卡时长计时 —— 
        if (timerCo != null) StopCoroutine(timerCo);
        float dur = Mathf.Max(1f, Current.levelDurationSeconds);

        string prefabName = (Current.enemyPrefab != null) ? Current.enemyPrefab.name : "<null>";
        Debug.Log($"[LevelRunner] Applied '{Current.levelName}'  dur={dur}s  spawn={Current.spawnInterval}s  prefab={prefabName}  window=[+{Current.spawnStartDelay}s, -{Current.spawnStopEarly}s]");

        timerCo = StartCoroutine(LevelTimerSeconds(dur));
    }

    IEnumerator LevelTimerSeconds(float seconds)
    {
        // 用实时计时，不受 Time.timeScale 影响
        yield return new WaitForSecondsRealtime(seconds);
        Debug.Log("[LevelRunner] Level end (manual duration)");
        OnLevelEnded?.Invoke();
    }

    // —— 跨关清场 —— 
    void CleanLevelState()
    {
        if (spawner) spawner.StopAndReset();
        
        Enemy.KillAll();
        
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var b in bullets) if (b) Destroy(b.gameObject);
        
        if (pattern) pattern.ResetForNewLevel();
        
    }

    // 开发期：N键跳过本关
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            OnLevelEnded?.Invoke();
    }
}
