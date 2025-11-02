using UnityEngine;
using Game.Skills; 

public sealed class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform    player;
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private SkillLibrary library;

    private SkillCastContext _ctx;

    void Awake()
    {
        if (!player)
        {
            var vs = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
            player = vs ? vs.transform : GameObject.FindWithTag("Player")?.transform;
        }
        if (!playerSkills) playerSkills = Object.FindFirstObjectByType<PlayerSkills>(FindObjectsInactive.Include);
        if (!library)      library      = Object.FindFirstObjectByType<SkillLibrary>(FindObjectsInactive.Include);

        _ctx = new SkillCastContext { Player = player, Runner = this };
    }

    void OnEnable()
    {
        HitJudge.OnPerfect += HandleHit;
        HitJudge.OnGood    += HandleHit;
    }

    void OnDisable()
    {
        HitJudge.OnPerfect -= HandleHit;
        HitJudge.OnGood    -= HandleHit;
    }

    void HandleHit()
    {
        if (!player || !playerSkills || !library) return;

        var actives = playerSkills.Actives;   // 仅记录已选技能
        for (int i = 0; i < actives.Count; i++)
        {
            var impl = library.GetImpl(actives[i]); // ★ 新库 API
            impl?.Cast(_ctx);
        }
    }
}