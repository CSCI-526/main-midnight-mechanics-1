using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    // Story/Pack（保留）
    public LevelPack SelectedPack { get; private set; }
    public int CurrentLevelIndex { get; private set; }

    // Challenge：只打一关
    public LevelConfig SelectedLevel { get; private set; }

    // 待应用的起始金币；<0 表示无
    public int PendingStartGold { get; private set; } = -1;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    // —— Story/Pack —— //
    public void BeginPack(LevelPack pack)
    {
        SelectedPack = pack;
        CurrentLevelIndex = 0;
        SelectedLevel = null;
        PendingStartGold = -1;
    }

    public LevelConfig GetCurrentLevel()
    {
        if (!SelectedPack || SelectedPack.levels == null || SelectedPack.levels.Count == 0) return null;
        int i = Mathf.Clamp(CurrentLevelIndex, 0, SelectedPack.levels.Count - 1);
        return SelectedPack.levels[i];
    }

    public bool TryAdvanceLevel()
    {
        if (!SelectedPack) return false;
        CurrentLevelIndex++;
        return CurrentLevelIndex < SelectedPack.levels.Count;
    }

    // —— Challenge —— //
    public void BeginChallenge(LevelPack pack)
    {
        SelectedPack = null;
        SelectedLevel = (pack && pack.levels != null && pack.levels.Count > 0) ? pack.levels[0] : null;
        SetPendingStartGold(pack ? pack.challengeStartGold : 0);
    }

    public void BeginChallenge(LevelConfig level, int startGold)
    {
        SelectedPack = null;
        SelectedLevel = level;
        SetPendingStartGold(startGold);
    }

    public void ClearChallenge()
    {
        SelectedLevel = null;
        PendingStartGold = -1;
    }

    public void SetPendingStartGold(int value)
    {
        PendingStartGold = Mathf.Max(0, value);
    }

    public int ConsumePendingStartGold()
    {
        int v = PendingStartGold;
        PendingStartGold = -1;
        return v;
    }
}
