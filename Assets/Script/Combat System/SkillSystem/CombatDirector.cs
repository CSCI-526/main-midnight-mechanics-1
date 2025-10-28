using UnityEngine;
using Game.Skills;
using static SkillLibrary;

/// <summary>Cast equipped active skills when SPACE note is hit.</summary>
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
        if (!player)       player       = FindObjectOfType<PlayerHealth>(true)?.transform;
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!library)      library      = FindObjectOfType<SkillLibrary>(true);
        _ctx = new SkillCastContext { Player = player, Runner = this };
    }

    void OnEnable()  => HitJudge.OnBasicHit += HandleSpaceHit;
    void OnDisable() => HitJudge.OnBasicHit -= HandleSpaceHit;

    private void HandleSpaceHit()
    {
        if (!player || !playerSkills || !library) return;

        var stats = playerSkills.GetCurrentStats();

        // optional always-cast
        if (alwaysCastSkills != null)
            for (int i = 0; i < alwaysCastSkills.Length; i++)
                alwaysCastSkills[i]?.Cast(_ctx, stats);

        // cast all equipped actives
        var actives = playerSkills.Actives;
        for (int i = 0; i < actives.Count; i++)
        {
            var impl = library.GetActiveImpl(actives[i]);
            if (impl != null) impl.Cast(_ctx, stats);
        }
    }
}