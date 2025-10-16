using UnityEngine;

public class AttackDispatcher : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Bullet bulletPrefab;

    [Header("Basic Shot Fan Spread")]
    [SerializeField] private float spreadDegreesPerBullet = 6f; // 多发时的夹角步进（可调）

    [Header("Refs")]
    [SerializeField] private PlayerSkills playerSkills; // ☆ 新增：读取被动

    void Awake()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>()?.transform;
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
    }

    void OnEnable()  => HitJudge.OnBasicHit += HandleBasicHit;
    void OnDisable() => HitJudge.OnBasicHit -= HandleBasicHit;

    void HandleBasicHit()
    {
        if (!bulletPrefab || !player) return;

        Enemy nearest = FindNearestEnemy();
        if (!nearest) return;

        // 读取被动聚合
        var stats = playerSkills ? playerSkills.GetCurrentStats() : default;
        int   count = Mathf.Max(1, stats.count);
        float speed = (stats.speed > 0f) ? stats.speed : bulletPrefab.DefaultSpeed; 

        // 基础方向
        Vector2 baseDir = (nearest.transform.position - player.position).normalized;
        if (baseDir.sqrMagnitude <= 0.0001f) baseDir = Vector2.right;

        // 计算扇形发射（count=1 时即直射）
        float mid = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - mid) * spreadDegreesPerBullet;
            Vector2 dir  = Rotate(baseDir, offset);

            var b = Instantiate(bulletPrefab);
            b.Configure(speed, stats.damage);        // 先覆盖速度/伤害（伤害后续用）
            b.FireDir(player.position, dir);         // 方向发射（不依赖目标点）
        }
    }

    Enemy FindNearestEnemy()
    {
        Enemy best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            float d = (e.transform.position - player.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
