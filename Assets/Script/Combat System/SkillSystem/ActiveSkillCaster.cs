using UnityEngine;
using static SkillLibrary;

/// <summary>
/// Casts equipped active skills when a valid skill trigger occurs.
/// Listens to HitJudge.OnBasicHit and checks PatternSystem completion.
/// </summary>
public class ActiveSkillCaster : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private PatternSystem pattern;

    [Header("Prefabs")]
    [SerializeField] private ChainBoltProjectile chainBoltPrefab;

    [Header("Tuning: ChainBolt")]
    [SerializeField] private float chainSearchRadius = 6f;
    [SerializeField] private int   chainBaseHops = 1;         // base bounces (per projectile)
    [SerializeField] private float chainLifeTime = 5f;

    void Awake()
    {
        if (!player)       player       = FindObjectOfType<PlayerHealth>(true)?.transform;
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!pattern)      pattern      = FindObjectOfType<PatternSystem>(true);
    }

    void OnEnable()  { HitJudge.OnBasicHit += HandleBasicHit; }
    void OnDisable() { HitJudge.OnBasicHit -= HandleBasicHit; }

    private void HandleBasicHit()
    {
        if (!playerSkills || !pattern) return;

        // Only cast actives when the pattern for this round is completed.
        if (!pattern.IsSequenceCompleted) return;

        var stats = playerSkills.GetCurrentStats();

        foreach (var id in playerSkills.Actives)
        {
            switch (id)
            {
                case ActiveSkillId.ChainBolt:
                    CastChainBolt(stats);
                    break;
                // Upcoming skills (implement in next steps):
                // case ActiveSkillId.Explosion: break;
                // case ActiveSkillId.OrbitOrb: break;
                // case ActiveSkillId.SpreadShot: break;
            }
        }
    }

    /// <summary>
    /// Fires one or more chain projectiles that bounce across enemies.
    /// Count and speed come from passive stats; hops scales with count.
    /// </summary>
    private void CastChainBolt(PlayerSkills.SkillStats stats)
    {
        if (!chainBoltPrefab || !player) return;

        // Use nearest enemy as initial direction anchor; if none, skip.
        Enemy first = FindNearestEnemy(player.position);
        if (!first) return;

        // Spawn "count" chain bolts. Each has hop budget based on count.
        int boltCount = Mathf.Max(1, stats.count);
        int hopBudget = Mathf.Max(0, chainBaseHops + (stats.count - 1)); // e.g., base 1, +1 per CountUp level

        // Spread the initial directions slightly to avoid perfect overlap.
        float spreadStep = 8f;
        float half = (boltCount - 1) * 0.5f;

        Vector2 baseDir = (first.transform.position - player.position).normalized;
        if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector2.right;

        for (int i = 0; i < boltCount; i++)
        {
            float offset = (i - half) * spreadStep;
            Vector2 dir = Rotate(baseDir, offset);

            var bolt = Instantiate(chainBoltPrefab);
            bolt.Initialize(
                startPosition: player.position,
                startDirection: dir,
                moveSpeed: Mathf.Max(0.1f, stats.speed),
                maxHops: hopBudget,
                searchRadius: chainSearchRadius + stats.area, // area slightly increases hop search radius
                lifeTime: chainLifeTime
            );
        }
    }

    private static Enemy FindNearestEnemy(Vector3 pos)
    {
        Enemy best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
