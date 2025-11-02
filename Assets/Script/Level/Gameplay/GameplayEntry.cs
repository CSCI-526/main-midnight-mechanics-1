using UnityEngine;

public class GameplayEntry : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private LevelPack   fallbackPack;
    [SerializeField] private SceneFlow   sceneFlow;

    [SerializeField] private ShopPanel   shopPanel;  // ← 只保留合并后的 ShopPanel
    [SerializeField] private GoldWallet  wallet;

    GameSession session;

    void Awake()
    {
        if (!runner)    runner    = FindFirstObjectByType<LevelRunner>(FindObjectsInactive.Include);
        if (!sceneFlow) sceneFlow = FindFirstObjectByType<SceneFlow>(FindObjectsInactive.Include);
        if (!shopPanel) shopPanel = FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (!wallet)    wallet    = FindFirstObjectByType<GoldWallet>(FindObjectsInactive.Include);

        session = GameSession.Instance ?? FindFirstObjectByType<GameSession>(FindObjectsInactive.Include);
    }

    void OnEnable()  { if (runner) runner.OnLevelEnded += HandleLevelEnded; }
    void OnDisable() { if (runner) runner.OnLevelEnded -= HandleLevelEnded; }

    void Start()
    {
        ApplyStartGoldIfPending();
        OpenShopThen(() => LoadCurrentLevel());
    }

    void ApplyStartGoldIfPending()
    {
        if (session == null) return;

        int startGold = session.ConsumePendingStartGold();
        if (startGold < 0) return;

        if (!wallet) wallet = FindFirstObjectByType<GoldWallet>(FindObjectsInactive.Include);
        if (!wallet) { Debug.LogWarning("[GameplayEntry] GoldWallet not found in scene."); return; }

        wallet.Set(startGold);
    }

    void LoadCurrentLevel()
    {
        if (session && session.SelectedLevel) { runner.Apply(session.SelectedLevel); return; }

        var level = session ? session.GetCurrentLevel()
                            : (fallbackPack ? (fallbackPack.levels.Count > 0 ? fallbackPack.levels[0] : null) : null);

        if (!level)
        {
            var pack = session ? session.SelectedPack : fallbackPack;
            if (pack == null || pack.levels == null || pack.levels.Count == 0)
            {
                Debug.LogError("[GameplayEntry] No LevelPack or empty levels.");
                return;
            }
            if (session && session.SelectedPack == null) session.BeginPack(pack);
            level = session.GetCurrentLevel();
        }

        if (!level) { Debug.LogError("[GameplayEntry] Current level is null"); return; }
        runner.Apply(level);
    }

    void HandleLevelEnded()
    {
        var finished = runner ? runner.Current : null;
        if (wallet && finished && finished.rewardGold > 0)
        {
            wallet.Add(finished.rewardGold);
            Debug.Log($"[GameplayEntry] Reward +{finished.rewardGold} gold for '{finished.levelName}'.");
        }

        if (session && session.SelectedLevel)
        {
            session.ClearChallenge();
            if (sceneFlow) sceneFlow.LoadLevelSelector();
            return;
        }

        if (session != null && session.TryAdvanceLevel())
            OpenShopThen(() => LoadCurrentLevel());
        else
            sceneFlow.LoadLevelSelector();
    }

    void OpenShopThen(System.Action next)
    {
        if (shopPanel) shopPanel.Show(next);
        else next?.Invoke();
    }
}
