using UnityEngine;

public class AttackDispatcher : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Bullet bulletPrefab;

    void Awake()
    {
        // 允许在 Inspector 留空：自动找场景里的 Player
        if (!player)
            player = FindObjectOfType<PlayerHealth>()?.transform;
    }

    void OnEnable()  => HitJudge.OnBasicHit += HandleBasicHit;
    void OnDisable() => HitJudge.OnBasicHit -= HandleBasicHit;

    void HandleBasicHit()
    {
        Enemy nearest = FindNearestEnemy();
        if (!nearest || !player || !bulletPrefab) return;

        var b = Instantiate(bulletPrefab);
        b.FireTowards(player.position, nearest.transform.position);
    }

    Enemy FindNearestEnemy()
    {
        Enemy best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            float d = (e.transform.position - player.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = e;
            }
        }
        return best;
    }
}