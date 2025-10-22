using System.Collections;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [SerializeField] private RhythmSystem  rhythm;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;
    [SerializeField] private AudioSource   music;

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;
    public LevelConfig Current { get; private set; }

    // --- 新增：关卡进度公开属性 ---
    public float LevelDuration { get; private set; }          // 秒
    public float ElapsedRealtime { get; private set; }        // 秒（实时）
    public float Progress01 => LevelDuration > 0f
        ? Mathf.Clamp01(ElapsedRealtime / LevelDuration) : 0f;

    Coroutine timerCo;
    float _startRealtime;
    bool  _running;

    public void Apply(LevelConfig c)
    {
        CleanLevelState();

        Current = c;
        if (!Current)
        {
            Debug.LogError("[LevelRunner] LevelConfig is null");
            return;
        }

        if (music)
        {
            music.Stop();
            music.clip         = Current.bgm;
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f;
            if (music.clip) music.Play();
        }

        float spb = 60f / Mathf.Max(1f, Current.bpm);
        rhythm.SetCycleSeconds(Mathf.Max(0.01f, Current.cycleBeats * spb));
        rhythm.hitCenter    = Current.hitCenter;
        rhythm.hitHalfWidth = Current.hitHalfWidth;

        pattern.SetSequenceLength(Current.sequenceLength);

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

        rhythm.ForceNextRound();
        OnLevelApplied?.Invoke();

        // --- 新增：进度初始化 + 实时计时 ---
        if (timerCo != null) StopCoroutine(timerCo);
        LevelDuration   = Mathf.Max(1f, Current.levelDurationSeconds);
        ElapsedRealtime = 0f;
        _startRealtime  = Time.realtimeSinceStartup;
        _running        = true;

        string prefabName = (Current.enemyPrefab != null) ? Current.enemyPrefab.name : "<null>";
        Debug.Log($"[LevelRunner] Applied '{Current.levelName}'  dur={LevelDuration}s  spawn={Current.spawnInterval}s  prefab={prefabName}  window=[+{Current.spawnStartDelay}s, -{Current.spawnStopEarly}s]");

        timerCo = StartCoroutine(LevelTimerSeconds(LevelDuration));
    }

    IEnumerator LevelTimerSeconds(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        _running        = false;
        ElapsedRealtime = LevelDuration;
        Debug.Log("[LevelRunner] Level end (manual duration)");
        OnLevelEnded?.Invoke();
    }

    void CleanLevelState()
    {
        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var b in bullets) if (b) Destroy(b.gameObject);
        if (pattern) pattern.ResetForNewLevel();
    }

    void Update()
    {
        // --- 新增：每帧刷新实时进度 ---
        if (_running)
        {
            ElapsedRealtime = Mathf.Clamp(Time.realtimeSinceStartup - _startRealtime, 0f, LevelDuration);
        }

        if (Input.GetKeyDown(KeyCode.N))
            OnLevelEnded?.Invoke();
    }
}
