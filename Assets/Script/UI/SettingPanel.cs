using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[DisallowMultipleComponent]
public class SettingPanel : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject panel;      // 

    [Header("Controls")]
    [SerializeField] private Slider masterSlider;   // Slider: Min=0, Max=1, WholeNumbers=false
    [SerializeField] private TMP_Text percentText;  // 

    [Header("Buttons")]
    [SerializeField] private Button openButton;     // 场景中“设置/齿轮”按钮
    [SerializeField] private Button closeButton;    // 面板里的“关闭/返回”按钮

    [Header("Defaults")]
    [Range(0f, 1f)]
    [SerializeField] private float initialVolume = 1f; // 初始音量

    private const string KEY = "master_volume";

    void Awake()
    {
        // 绑定按钮（若未指定则忽略）
        if (openButton)  openButton.onClick.AddListener(OpenPanel);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);

        // 确保滑条有回调 —— 这就是你之前“关了又变 100%”的根因
        if (masterSlider)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.wholeNumbers = false;
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        // 读存档或应用初始音量
        float v = PlayerPrefs.HasKey(KEY) ? PlayerPrefs.GetFloat(KEY, initialVolume) : Mathf.Clamp01(initialVolume);
        AudioListener.volume = Mathf.Clamp01(v);

        // 同步 UI
        if (masterSlider) masterSlider.SetValueWithoutNotify(AudioListener.volume);
        UpdateLabel();

        // 默认隐藏面板
        if (panel) panel.SetActive(false);

        // 第一次运行没有存档时写入一份
        if (!PlayerPrefs.HasKey(KEY)) Save();
    }

    void OnDestroy()
    {
        if (openButton)  openButton.onClick.RemoveListener(OpenPanel);
        if (closeButton) closeButton.onClick.RemoveListener(ClosePanel);
        if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
    }

    // —— 面板控制 —— //
    public void OpenPanel()
    {
        if (!panel) return;
        panel.SetActive(true);
        if (masterSlider) masterSlider.SetValueWithoutNotify(AudioListener.volume);
        UpdateLabel();
    }

    public void ClosePanel()
    {
        if (!panel) return;
        panel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (!panel) return;
        if (panel.activeSelf) ClosePanel(); else OpenPanel();
    }

    // —— 滑条回调（在 Awake 里已自动绑定）—— //
    public void OnMasterChanged(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        Save();
        UpdateLabel();
    }

    // 可选：重置到“初始音量”（可绑到“重置”按钮）
    public void ResetToInitial()
    {
        float v = Mathf.Clamp01(initialVolume);
        AudioListener.volume = v;
        Save();
        if (masterSlider) masterSlider.SetValueWithoutNotify(v);
        UpdateLabel();
    }

    // —— 辅助 —— //
    private void Save()
    {
        PlayerPrefs.SetFloat(KEY, AudioListener.volume);
        PlayerPrefs.Save();
    }

    private void UpdateLabel()
    {
        if (percentText)
            percentText.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }
}
