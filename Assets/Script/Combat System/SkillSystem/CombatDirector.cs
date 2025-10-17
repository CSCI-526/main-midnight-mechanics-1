using UnityEngine;
using Game.Skills;
using static SkillLibrary;

/// <summary>Central dispatcher for basic + equipped active skills.</summary>
public sealed class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform     player;
    [SerializeField] private PlayerSkills  playerSkills;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private SkillLibrary  library;

    [Header("Always Cast (hidden skills)")]
    [SerializeField] private ActiveSkillBase[] alwaysCastSkills; // e.g., BasicShotSkill

    [Header("Debug: Grant on Start (by implementation)")]
    [SerializeField] private ActiveSkillBase[] debugGrantOnStart; // drag skill SOs here

    private SkillCastContext _ctx;

    private void Awake()
    {
        if (!player)       player       = FindObjectOfType<PlayerHealth>(true)?.transform;
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!pattern)      pattern      = FindObjectOfType<PatternSystem>(true);

        _ctx = new SkillCastContext { Player = player, Runner = this };
    }

    private void Start()
    {
        if (playerSkills == null || library == null || debugGrantOnStart == null) return;

        for (int i = 0; i < debugGrantOnStart.Length; i++)
        {
            var impl = debugGrantOnStart[i];
            if (impl == null) continue;
            if (library.TryGetActiveIdByImpl(impl, out var id))
                playerSkills.TryAddOrLevelUp(id);
            else
                Debug.LogWarning($"[CombatDirector] Impl not found in library: {impl.name}");
        }
    }

    private void OnEnable()  => HitJudge.OnBasicHit += OnBasicHit;
    private void OnDisable() => HitJudge.OnBasicHit -= OnBasicHit;

    private void OnBasicHit()
    {
        if (!player || !playerSkills) return;

        var stats = playerSkills.GetCurrentStats();

        // Hidden skills always cast (e.g., basic shot)
        if (alwaysCastSkills != null)
            for (int i = 0; i < alwaysCastSkills.Length; i++)
                alwaysCastSkills[i]?.Cast(_ctx, stats);

        // Actives require the pattern round to be completed
        if (pattern != null && !pattern.IsSequenceCompleted) return;
        if (library == null) return;

        var actives = playerSkills.Actives;
        for (int i = 0; i < actives.Count; i++)
        {
            var impl = library.GetActiveImpl(actives[i]);
            if (impl != null) impl.Cast(_ctx, stats);
        }
    }
}
