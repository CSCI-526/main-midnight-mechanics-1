using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Keyboard")]
public sealed class KeyboardSkill : ActiveSkillBase
{
    [Header("Key Shots")]
    [SerializeField] private KeyboardProjectile projectilePrefab;
    [SerializeField] private int   keyCount        = 5;
    [SerializeField] private float keySpeed        = 16f;
    [SerializeField] private int   keyDamage       = 2;
    [SerializeField] private float coneHalfAngle   = 18f;
    [SerializeField] private float spawnOffset     = 0.1f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin  = ctx.Player.position;
        Vector2 baseDir = SkillUtil.AimDirOrRandomUp(origin);
        int count = Mathf.Max(1, keyCount);
        float half = Mathf.Max(0f, coneHalfAngle);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i / (float)(count - 1)) * 2f - 1f;  // [-1,1]
            float ang = t * half;
            Vector2 dir = SkillUtil.Rotate(baseDir, ang).normalized;
            Vector2 start = origin + dir * Mathf.Max(0f, spawnOffset);

            var k = Object.Instantiate(projectilePrefab);
            k.Configure(keySpeed, Mathf.Max(1, keyDamage));
            k.FireDir(start, dir);
        }
    }
}