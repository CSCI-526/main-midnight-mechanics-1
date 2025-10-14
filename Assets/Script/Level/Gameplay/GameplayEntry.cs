using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayEntry : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private LevelPack fallbackPack;
    [SerializeField] private SceneFlow sceneFlow;

    [Header("UI")]
    [SerializeField] private ShopUI shopUI; 
    [SerializeField] private GameOverUI gameOverUI; 

    GameSession session;
    PlayerHealth player;

    void Awake()
    {
        if (!runner) runner = FindObjectOfType<LevelRunner>();
        if (!sceneFlow) sceneFlow = FindObjectOfType<SceneFlow>(true);
        session = GameSession.Instance ?? FindObjectOfType<GameSession>();
        player = FindObjectOfType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (runner) runner.OnLevelEnded += HandleLevelEnded;
        if (player) player.OnDied += HandlePlayerDied; 
    }

    void OnDisable()
    {
        if (runner) runner.OnLevelEnded -= HandleLevelEnded;
        if (player) player.OnDied -= HandlePlayerDied;
    }

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

    // —— 关卡结束：先弹商店；点 Next 再继续/回选单 —— 
    void HandleLevelEnded()
    {
        if (!shopUI)
        {
            Debug.LogWarning("[GameplayEntry] ShopUI not assigned, auto-continue.");
            ContinueAfterShop();
            return;
        }

        shopUI.Show(ContinueAfterShop);
    }

    void ContinueAfterShop()
    {
        if (session != null && session.TryAdvanceLevel())
        {
            LoadCurrentLevel();
        }
        else
        {
            if (sceneFlow) sceneFlow.LoadLevelSelector();
            else SceneManager.LoadScene("LevelSelector", LoadSceneMode.Single);
        }
    }

    // —— 玩家死亡：弹 GameOver —— 
    void HandlePlayerDied()
    {
        if (gameOverUI) gameOverUI.Show();
        else
        {
            // 极简兜底：直接回选单
            Time.timeScale = 1f;
            if (sceneFlow) sceneFlow.LoadLevelSelector();
            else SceneManager.LoadScene("LevelSelector", LoadSceneMode.Single);
        }
    }
}
