using UnityEngine;

public class GameplayEntry : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private LevelPack fallbackPack;
    [SerializeField] private SceneFlow sceneFlow;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private GoldWallet wallet;

    GameSession session;

    void Awake()
    {
        if (!runner)    runner    = FindFirstObjectByType<LevelRunner>(FindObjectsInactive.Include);
        if (!sceneFlow) sceneFlow = FindFirstObjectByType<SceneFlow>(FindObjectsInactive.Include);
        if (!shopUI)    shopUI    = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (!shopPanel) shopPanel = FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (!wallet)    wallet    = FindFirstObjectByType<GoldWallet>(FindObjectsInactive.Include);

        session = GameSession.Instance ?? FindFirstObjectByType<GameSession>(FindObjectsInactive.Include);
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
        var finished = runner ? runner.Current : null;
        if (wallet && finished && finished.rewardGold > 0)
        {
            wallet.Add(finished.rewardGold);
            Debug.Log($"[GameplayEntry] Reward +{finished.rewardGold} gold for '{finished.levelName}'.");
        }
        
        if (session != null && session.TryAdvanceLevel())
            OpenShopThen(() => LoadCurrentLevel());
        else
            sceneFlow.LoadLevelSelector();
    }

    void OpenShopThen(System.Action next)
    {
        if (shopPanel) shopPanel.BuildOffers();
        if (shopUI) shopUI.Show(next);
        else next?.Invoke();
    }
}
