using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public LevelPack SelectedPack { get; private set; }
    public int CurrentLevelIndex { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 选了某个专辑（曲库）就从第0关开始
    public void BeginPack(LevelPack pack)
    {
        SelectedPack = pack;
        CurrentLevelIndex = 0;
    }

    public LevelConfig GetCurrentLevel()
    {
        if (!SelectedPack || SelectedPack.levels == null || SelectedPack.levels.Count == 0) return null;
        int i = Mathf.Clamp(CurrentLevelIndex, 0, SelectedPack.levels.Count - 1);
        return SelectedPack.levels[i];
    }

    // 
    public bool TryAdvanceLevel()
    {
        if (!SelectedPack) return false;
        CurrentLevelIndex++;
        return CurrentLevelIndex < SelectedPack.levels.Count;
    }
}