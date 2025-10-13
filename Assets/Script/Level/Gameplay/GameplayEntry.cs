using UnityEngine;

public class GameplayEntry : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private LevelPack fallbackPack; // 直接进Gameplay时用的兜底
    [SerializeField] private SceneFlow sceneFlow;    // 用于回到 LevelSelector

    GameSession session;

    void Awake()
    {
        if (!runner) runner = FindObjectOfType<LevelRunner>();
        if (!sceneFlow) sceneFlow = FindObjectOfType<SceneFlow>(true);
        session = GameSession.Instance ?? FindObjectOfType<GameSession>();
    }

    void OnEnable()  { if (runner) runner.OnLevelEnded += HandleLevelEnded; }
    void OnDisable() { if (runner) runner.OnLevelEnded -= HandleLevelEnded; }

    void Start()
    {
        var pack = session ? session.SelectedPack : null;
        if (!pack) pack = fallbackPack;

        if (pack == null || pack.levels == null || pack.levels.Count == 0)
        {
            Debug.LogError("[GameplayEntry] No LevelPack or empty levels.");
            return;
        }
        
        if (session && session.SelectedPack == null) session.BeginPack(pack);

        LoadCurrentLevel();
    }

    void LoadCurrentLevel()
    {
        var level = session ? session.GetCurrentLevel() : (fallbackPack ? fallbackPack.levels[0] : null);
        if (!level) { Debug.LogError("[GameplayEntry] Current level is null"); return; }
        runner.Apply(level);
    }

    void HandleLevelEnded()
    {
        if (session != null && session.TryAdvanceLevel())
        {
            LoadCurrentLevel();
        }
        else
        {
            sceneFlow.LoadLevelSelector();
        }
    }
}