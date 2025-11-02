using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject rootPanel;          // 暂停菜单根（遮罩+按钮）
    [SerializeField] private Button     resumeBtn;
    [SerializeField] private Button     settingsBtn;
    [SerializeField] private Button     backToSelectBtn;
    [SerializeField] private Button     exitBtn;

    [Header("Re-use Settings (from Menu scene)")]
    [Tooltip("把你菜单场景里的 SettingPanel 预制体实例（或拷贝）拖进来")]
    [SerializeField] private GameObject settingsPanelRoot;

    [Header("Scenes")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private string menuSceneName        = "Menu";

    // 本地缓存：用于侦测 Settings 的开/关
    SettingPanel _settings;
    bool _settingsWasOpenLastFrame;

    void Awake()
    {
        if (rootPanel) rootPanel.SetActive(false);

        if (resumeBtn)        resumeBtn.onClick.AddListener(Hide);
        if (settingsBtn)      settingsBtn.onClick.AddListener(OpenSettings);
        if (backToSelectBtn)  backToSelectBtn.onClick.AddListener(() => GoScene(levelSelectSceneName));
        if (exitBtn)          exitBtn.onClick.AddListener(() => GoScene(menuSceneName));

        if (settingsPanelRoot)
        {
            _settings = settingsPanelRoot.GetComponent<SettingPanel>();
            // 确保一开始不显示（菜单里可以显示，关卡里默认隐藏）
            settingsPanelRoot.SetActive(false);
        }
    }

    void OnEnable()
    {
        GamePause.OnPauseChanged += OnPauseChanged;
    }
    void OnDisable()
    {
        GamePause.OnPauseChanged -= OnPauseChanged;
    }

    void Update()
    {
        // ESC 行为：优先从 Settings 返回暂停菜单；否则开/关暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanelRoot && settingsPanelRoot.activeSelf)
            {
                // 关闭设置 → 回到暂停菜单（保持暂停状态）
                if (_settings) _settings.ClosePanel(); else settingsPanelRoot.SetActive(false);
                if (rootPanel) rootPanel.SetActive(true);
                return;
            }

            if (GamePause.IsPaused) Hide();
            else                    Show();
        }

        // 若暂停期间，Settings 从“开”->“关”（比如点了设置里的关闭按钮），自动回到暂停菜单
        bool settingsOpen = settingsPanelRoot && settingsPanelRoot.activeSelf;
        if (GamePause.IsPaused && _settingsWasOpenLastFrame && !settingsOpen)
        {
            if (rootPanel && !rootPanel.activeSelf) rootPanel.SetActive(true);
        }
        _settingsWasOpenLastFrame = settingsOpen;
    }

    public void Show()
    {
        if (rootPanel) rootPanel.SetActive(true);
        GamePause.SetPaused(true);
    }

    public void Hide()
    {
        if (rootPanel) rootPanel.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        GamePause.SetPaused(false);
    }

    void OpenSettings()
    {
        if (!settingsPanelRoot) return;

        // 隐藏暂停菜单 → 打开设置（仍保持暂停）
        if (rootPanel) rootPanel.SetActive(false);
        if (_settings) _settings.OpenPanel(); else settingsPanelRoot.SetActive(true);
    }

    void GoScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        GamePause.SetPaused(false); // 先解除暂停，避免把 TS=0 带进新场景
        SceneManager.LoadScene(sceneName);
    }

    void OnPauseChanged(bool on)
    {
        // 可在这里做额外 UI 切换或提示；当前不需要处理
    }
}
