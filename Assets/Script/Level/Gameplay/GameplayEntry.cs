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
        if (!runner)    runner    = FindObjectOfType<LevelRunner>(true);
        if (!sceneFlow) sceneFlow = FindObjectOfType<SceneFlow>(true);
        if (!shopUI)    shopUI    = FindObjectOfType<ShopUI>(true);
        if (!shopPanel) shopPanel = FindObjectOfType<ShopPanel>(true);
        if (!wallet)    wallet    = FindObjectOfType<GoldWallet>(true); // ★ 自动查找

        session = GameSession.Instance ?? FindObjectOfType<GameSession>(true);
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
        if (session != null && session.TryAdvanceLevel())
            OpenShopThen(() => LoadCurrentLevel());
        else
            sceneFlow.LoadLevelSelector();
    }

    void OpenShopThen(System.Action next)
    {
        var justFinished = runner ? runner.Current : null;
        if (wallet && justFinished && justFinished.rewardGold > 0)
            wallet.Add(justFinished.rewardGold);
        
        if (shopPanel) shopPanel.BuildOffers();

        if (shopUI) shopUI.Show(next);
        else next?.Invoke();
    }
}
