using UnityEngine;
using Game.Skills;
using static SkillLibrary;

/// <summary>
/// Cast equipped *active* skills when a note is judged as Perfect or Good.
/// Base attack removed; your "actives from shop" ARE the normal attack now.
/// </summary>
public sealed class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform    player;
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private SkillLibrary library;

    [Header("Always Cast (optional hidden skills)")]
    [SerializeField] private ActiveSkillBase[] alwaysCastSkills;

    private SkillCastContext _ctx;

    void Awake()
    {
        // Player
        if (!player)
        {
            var vs = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
            if (vs) player = vs.transform;
            else
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged) player = tagged.transform;
            }
        }

        // Other refs
        if (!playerSkills) playerSkills = Object.FindFirstObjectByType<PlayerSkills>(FindObjectsInactive.Include);
        if (!library)      library      = Object.FindFirstObjectByType<SkillLibrary>(FindObjectsInactive.Include);

        _ctx = new SkillCastContext { Player = player, Runner = this };
    }

    void OnEnable()
    {
        HitJudge.OnPerfect += HandleHit;   // ← 成功命中触发普通攻击
        HitJudge.OnGood    += HandleHit;
    }

    void OnDisable()
    {
        HitJudge.OnPerfect -= HandleHit;
        HitJudge.OnGood    -= HandleHit;
    }

    private void HandleHit()
    {
        if (!player || !playerSkills || !library) return;

        var stats = playerSkills.GetCurrentStats();

        // 可选：每次也顺带施放隐藏技能
        if (alwaysCastSkills != null)
            for (int i = 0; i < alwaysCastSkills.Length; i++)
                alwaysCastSkills[i]?.Cast(_ctx, stats);

        // 把“已装备的主动技能”视为“普通攻击”
        var actives = playerSkills.Actives;
        for (int i = 0; i < actives.Count; i++)
        {
            var impl = library.GetActiveImpl(actives[i]);
            if (impl != null) impl.Cast(_ctx, stats);
        }
    }
}
