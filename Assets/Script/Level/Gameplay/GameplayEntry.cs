using UnityEngine;

public class GameplayEntry : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private LevelPack   fallbackPack;
    [SerializeField] private SceneFlow   sceneFlow;
    [SerializeField] private ShopUI      shopUI;
    [SerializeField] private ShopPanel   shopPanel;
    [SerializeField] private GameOverUI  gameOverUI; 

    GameSession  session;
    PlayerHealth player;

    void Awake()
    {
        if (!runner)     runner     = FindObjectOfType<LevelRunner>(true);
        if (!sceneFlow)  sceneFlow  = FindObjectOfType<SceneFlow>(true);
        if (!shopUI)     shopUI     = FindObjectOfType<ShopUI>(true);
        if (!shopPanel)  shopPanel  = FindObjectOfType<ShopPanel>(true);
        if (!gameOverUI) gameOverUI = FindObjectOfType<GameOverUI>(true);

        session = GameSession.Instance ?? FindObjectOfType<GameSession>(true);
        player  = FindObjectOfType<PlayerHealth>(true);
    }

    void OnEnable()
    {
        if (runner) runner.OnLevelEnded += HandleLevelEnded;
        if (player) player.OnDied       += HandlePlayerDied;
    }

    void OnDisable()
    {
        if (runner) runner.OnLevelEnded -= HandleLevelEnded;
        if (player) player.OnDied       -= HandlePlayerDied;
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

        // 开场先进入商店
        OpenShopThen(() => LoadCurrentLevel());
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
            OpenShopThen(() => LoadCurrentLevel());
        else
            sceneFlow.LoadLevelSelector();
    }

    void HandlePlayerDied()
    {
        // 停止本关逻辑并清场
        if (runner) runner.AbortLevel();
        
        var hj = FindObjectOfType<HitJudge>(true);
        if (hj) hj.enabled = false;

        // 弹出 GameOver
        if (gameOverUI)
        {
            gameOverUI.Show(
                onBackToSelect: () => sceneFlow.LoadLevelSelector(),
                onRetry: () =>
                {
                    // 重置玩家，重开当前关
                    if (player) player.ResetFull();
                    var hj2 = FindObjectOfType<HitJudge>(true);
                    if (hj2) hj2.enabled = true;
                    LoadCurrentLevel();
                });
        }
        else
        {
            // 没配 UI 时直接回选择页
            sceneFlow.LoadLevelSelector();
        }
    }

    void OpenShopThen(System.Action next)
    {
        if (shopPanel) shopPanel.BuildOffers();
        if (shopUI) shopUI.Show(next);
        else next?.Invoke();
    }
}
