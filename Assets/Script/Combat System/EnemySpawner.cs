using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float radiusMin = 8f;
    [SerializeField] private float radiusMax = 10f;
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private Transform player; // 可空：自动查找场景里的 PlayerHealth

    private float timer;

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        timer = spawnInterval * 0.5f; // 让第一只稍快刷出
    }

    void Update()
    {
        if (!enemyPrefab) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnOne();
            timer = spawnInterval;
        }
    }

    void SpawnOne()
    {
        // 随机一个环状位置
        float r = Random.Range(radiusMin, radiusMax);
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;

        var e = Instantiate(enemyPrefab, pos, Quaternion.identity);

        // 显式传参：目标 & 速度
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        e.SetTarget(player);
        e.SetMoveSpeed(moveSpeed);
    }
}