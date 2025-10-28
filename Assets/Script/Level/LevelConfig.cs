using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Basic")]
    public string levelName = "Level";

    [Header("Rhythm")]
    public RhythmChart chart;            // 必填：本关谱面（含 bpm、拍号、音符时刻、默认领跑时间、关卡时长/用曲长）

    [Header("Spawning")]
    public Enemy enemyPrefab;            // 可空：无刷怪就留空
    public float spawnInterval = 1.5f;   // >0 生效
    public float spawnStartDelay = 0f;   // 开场延迟多少秒后开始刷怪
    public float spawnStopEarly  = 0f;   // 结束前提前多少秒停止刷怪

    [Header("Rewards")]
    [Min(0)] public int rewardGold = 20; // 关卡奖励
}