using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Config", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("Meta")]
    public string levelName = "Remix-1";
    public float levelDurationSeconds = 90f;

    [Header("Music")]
    public AudioClip bgm;
    [Tooltip("BPM：每分钟节拍数")]
    public float bpm = 120f;
    [Tooltip("指示球从左到右需要的拍数")]
    public float cycleBeats = 1f;

    [Header("Rhythm UI")]
    [Range(0f, 1f)] public float hitCenter = 0.82f;
    [Range(0f, 0.5f)] public float hitHalfWidth = 0.04f;

    [Header("Input Pattern")]
    [Min(1)] public int sequenceLength = 3;

    [Header("Spawning（简版）")]
    public Enemy enemyPrefab;
    [Min(0.1f)] public float spawnInterval = 1.5f;
}