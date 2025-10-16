using UnityEngine;
using static SkillLibrary;

/// <summary>
/// Centralized skill dispatcher for basic attack and equipped active skills.
/// Always fires basic shot on valid hit; casts actives only if the pattern round is completed.
/// Basic shot is not shown in HUD / shop, but shares the same passive scaling path.
/// </summary>
public sealed class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform     player;
    [SerializeField] private PlayerSkills  playerSkills;
    [SerializeField] private PatternSystem pattern;

    [Header("Basic Shot (built-in; not shown in HUD)")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField, Tooltip("Angle step between multiple basic bullets when CountUp > 0.")]
    private float basicSpreadStepDeg = 6f;

    [Header("ChainBolt (active skill)")]
    [SerializeField] private ChainBoltProjectile chainBoltPrefab;
    [SerializeField, Tooltip("Search radius for choosing next hop target (affected slightly by AreaUp).")]
    private float chainSearchRadius = 6f;
    [SerializeField, Tooltip("Base hop count per projectile; total hops = base + (count-1).")]
    private int chainBaseHops = 1;
    [SerializeField] private float chainLifeTime = 5f;
    [SerializeField, Tooltip("Initial direction fan-out step when firing multiple bolts.")]
    private float chainSpreadStepDeg = 8f;

    [Header("Debug Starter (optional)")]
    [SerializeField, Tooltip("Grant these active skills at Start for testing UI/flow.")]
    private ActiveSkillId[] grantActivesOnStart;

    private void Awake()
    {
        if (!player)       player       = FindObjectOfType<PlayerHealth>(true)?.transform;
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!pattern)      pattern      = FindObjectOfType<PatternSystem>(true);
    }

    private void Start()
    {
        // Optional: grant test skills so HUD shows icons and casting is enabled.
        if (playerSkills != null && grantActivesOnStart != null)
        {
            for (int i = 0; i < grantActivesOnStart.Length; i++)
                playerSkills.TryAddOrLevelUp(grantActivesOnStart[i]);
        }
    }

    private void OnEnable()
    {
        HitJudge.OnBasicHit += OnBasicHit;
    }

    private void OnDisable()
    {
        HitJudge.OnBasicHit -= OnBasicHit;
    }

    /// <summary>
    /// Called when player hits in the hit window. Always fires basic shot.
    /// Casts actives only if pattern round is completed.
    /// </summary>
    private void OnBasicHit()
    {
        if (!player || !playerSkills) return;

        // 1) Basic shot (always)
        FireBasicShot();

        // 2) Actives only when the current pattern round is completed
        if (pattern != null && !pattern.IsSequenceCompleted)
            return;

        var actives = playerSkills.Actives;
        for (int i = 0; i < actives.Count; i++)
        {
            switch (actives[i])
            {
                case ActiveSkillId.ChainBolt:
                    CastChainBolt();
                    break;

                // TODO (next steps):
                // case ActiveSkillId.Explosion: CastExplosion(); break;
                // case ActiveSkillId.OrbitOrb : CastOrbitOrb();  break;
                // case ActiveSkillId.SpreadShot: CastSpreadShot(); break;
            }
        }
    }

    /// <summary>
    /// Fires one or more basic bullets towards the nearest enemy.
    /// Scales with passives: CountUp (projectile count), SpeedUp (projectile speed).
    /// </summary>
    private void FireBasicShot()
    {
        if (!bulletPrefab || !player) return;

        Enemy nearest = FindNearestEnemy(player.position);
        if (!nearest) return;

        var stats  = playerSkills.GetCurrentStats();
        int count  = Mathf.Max(1, stats.count);
        float spd  = (stats.speed > 0f) ? stats.speed : bulletPrefab.DefaultSpeed;

        Vector2 baseDir = (nearest.transform.position - player.position).normalized;
        if (baseDir.sqrMagnitude < 1e-6f) baseDir = Vector2.right;

        float half = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - half) * basicSpreadStepDeg;
            Vector2 dir  = Rotate(baseDir, offset);

            var b = Instantiate(bulletPrefab);
            b.Configure(spd, stats.damage);           // damage reserved for future enemy HP system
            b.FireDir(player.position, dir);
        }
    }

    /// <summary>
    /// Casts ChainBolt projectiles that hop across enemies.
    /// Scales with passives: CountUp (number of bolts and hop budget), SpeedUp (bolt speed), AreaUp (search radius bonus).
    /// </summary>
    private void CastChainBolt()
    {
        if (!chainBoltPrefab || !player) return;

        Enemy nearest = FindNearestEnemy(player.position);
        if (!nearest) return;

        var stats     = playerSkills.GetCurrentStats();
        int count     = Mathf.Max(1, stats.count);
        int hopBudget = Mathf.Max(0, chainBaseHops + (stats.count - 1));
        float speed   = Mathf.Max(0.1f, stats.speed);
        float radius  = Mathf.Max(0.1f, chainSearchRadius + stats.area);

        Vector2 baseDir = (nearest.transform.position - player.position).normalized;
        if (baseDir.sqrMagnitude < 1e-6f) baseDir = Vector2.right;

        float half = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - half) * chainSpreadStepDeg;
            Vector2 dir  = Rotate(baseDir, offset);

            var bolt = Instantiate(chainBoltPrefab);
            bolt.Initialize(
                startPosition: player.position,
                startDirection: dir,
                moveSpeed: speed,
                maxHops: hopBudget,
                searchRadius: radius,
                lifeTime: chainLifeTime
            );
        }
    }

    private static Enemy FindNearestEnemy(Vector3 origin)
    {
        Enemy best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * c * 0f + v.x * s + v.y * c); // avoid allocation
    }
}
