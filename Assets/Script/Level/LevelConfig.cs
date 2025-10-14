using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Basic")]
    public string levelName = "Level";
    public AudioClip bgm;
    public float bpm = 120f;
    public int   cycleBeats = 1;
    [Range(0f, 1f)] public float hitCenter = 0.82f;
    [Range(0f, 0.5f)] public float hitHalfWidth = 0.04f;
    public int sequenceLength = 3;

    [Header("Duration")]
    public float levelDurationSeconds = 90f;

    [Header("Spawning")]
    public Enemy enemyPrefab;
    public float spawnInterval = 1.5f;

    [Tooltip("开场延迟多少秒后才开始刷怪")]
    public float spawnStartDelay = 0f;

    [Tooltip("在关卡结束前，提前多少秒停止刷怪")]
    public float spawnStopEarly = 0f;
}