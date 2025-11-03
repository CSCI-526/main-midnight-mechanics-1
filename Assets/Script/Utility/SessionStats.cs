using UnityEngine;

public class SessionStats : MonoBehaviour
{
    [Header("Optional refs (auto-find if empty)")]
    [SerializeField] private ViewerSystem     viewers;
    [SerializeField] private LiveRewardTicker ticker;
    [SerializeField] private LevelRunner      runner;

    public int PerfectCount  { get; private set; }
    public int GoodCount     { get; private set; }
    public int MissCount     { get; private set; }
    public int TopViewers    { get; private set; }
    public int DonateUSD     { get; private set; }   // 这里先用 LiveRewardTicker 的总收入

    void Awake()
    {
        if (!viewers) viewers = FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        if (!ticker)  ticker  = FindFirstObjectByType<LiveRewardTicker>(FindObjectsInactive.Include);
        if (!runner)  runner  = FindFirstObjectByType<LevelRunner>(FindObjectsInactive.Include);
    }

    void OnEnable()
    {
        // 命中统计
        HitJudge.OnPerfect += IncPerfect;
        HitJudge.OnGood    += IncGood;
        HitJudge.OnMiss    += IncMiss;

        // 观众峰值
        if (viewers) viewers.OnViewersChanged += TrackTopViewers;

        // 捐赠（用总收入代理）
        if (ticker) ticker.OnEarningsChanged += SetDonate;

        // 关卡应用时重置
        if (runner) runner.OnLevelApplied += ResetStats;

        // 场景刚进来也先清一次
        ResetStats();
    }

    void OnDisable()
    {
        HitJudge.OnPerfect -= IncPerfect;
        HitJudge.OnGood    -= IncGood;
        HitJudge.OnMiss    -= IncMiss;
        if (viewers) viewers.OnViewersChanged -= TrackTopViewers;
        if (ticker)  ticker.OnEarningsChanged -= SetDonate;
        if (runner)  runner.OnLevelApplied    -= ResetStats;
    }

    public void ResetStats()
    {
        PerfectCount = GoodCount = MissCount = 0;
        TopViewers   = viewers ? Mathf.Max(TopViewers, viewers.Current) : 0;
        DonateUSD    = ticker ? ticker.TotalEarningsUSD : 0;
    }

    void IncPerfect() => PerfectCount++;
    void IncGood()    => GoodCount++;
    void IncMiss()    => MissCount++;

    void TrackTopViewers(int current)
    {
        if (current > TopViewers) TopViewers = current;
    }

    void SetDonate(int totalUsd)
    {
        // totalUsd 是累计，本局内会不断更新
        DonateUSD = totalUsd;
    }
}
