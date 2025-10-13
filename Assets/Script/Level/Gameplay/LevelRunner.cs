using System.Collections;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [SerializeField] private RhythmSystem rhythm;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private AudioSource music;

    public System.Action OnLevelEnded;

    LevelConfig current;
    Coroutine timerCo;

    public void Apply(LevelConfig c)
    {
        current = c;
        if (!current) { Debug.LogError("[LevelRunner] LevelConfig is null"); return; }

        // --- 音乐 ---
        if (music)
        {
            music.Stop();
            music.clip = current.bgm;
            if (music.clip) music.Play();
        }

        // --- 节奏（BPM→秒）---
        float spb = 60f / Mathf.Max(1f, current.bpm);
        rhythm.SetCycleSeconds(Mathf.Max(0.01f, current.cycleBeats * spb));
        rhythm.hitCenter = current.hitCenter;
        rhythm.hitHalfWidth = current.hitHalfWidth;

        // --- 输入长度 ---
        pattern.SetSequenceLength(current.sequenceLength);

        // --- 刷怪 ---
        if (current.enemyPrefab) spawner.SetEnemyPrefab(current.enemyPrefab);
        spawner.SetSpawnInterval(current.spawnInterval);

        // --- 重启关卡计时 ---
        if (timerCo != null) StopCoroutine(timerCo);
        timerCo = StartCoroutine(LevelTimer());
    }

    IEnumerator LevelTimer()
    {
        // 优先用音乐播放完作为关卡结束；没有音乐就用 levelDurationSeconds
        if (music && music.clip)
        {
            // 等待音乐播放完
            while (music.isPlaying) yield return null;
        }
        else
        {
            float dur = current ? Mathf.Max(5f, current.levelDurationSeconds) : 60f;
            yield return new WaitForSecondsRealtime(dur);
        }

        OnLevelEnded?.Invoke();
    }

    // 开发期快捷键：N=跳过当前关
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            OnLevelEnded?.Invoke();
        }
    }
}