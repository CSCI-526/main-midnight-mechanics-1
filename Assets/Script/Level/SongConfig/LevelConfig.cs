using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Basic")]
    public string levelName = "Level";

    [Header("Rhythm")]
    public RhythmChart chart;                    // 只负责谱面（bpm/拍号/slot等）

    [Header("Audio & Duration")]
    public AudioClip bgm;                        // 本关音乐
    [Min(0f)] public float levelDurationSeconds = 90f;  // 手动时长
    [Min(0f)] public float bgmDelaySec = 0f;             // 开局延迟播放 BGM（仅延迟音乐）

    [Header("Spawning (per-level)")]
    public Enemy enemyPrefab;                    // 本关用的敌人预制体（可空）
    [Min(0.01f)] public float spawnInterval = 1.5f;
    [Min(0f)]    public float spawnStartDelay = 0f;      // 开场后延迟多少秒开始刷
    [Min(0f)]    public float spawnStopEarly  = 0f;      // 结束前提前多少秒停止刷

    [Header("Rewards")]
    [Min(0)] public int rewardGold = 20;
}