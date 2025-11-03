using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class SettingPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panel;

    [Header("Master")]
    [SerializeField] private Slider   masterSlider;
    [SerializeField] private TMP_Text masterPercent;

    [Header("BGM")]
    [SerializeField] private Slider   bgmSlider;
    [SerializeField] private TMP_Text bgmPercent;

    [Header("SFX")]
    [SerializeField] private Slider   sfxSlider;
    [SerializeField] private TMP_Text sfxPercent;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    [Header("Defaults")]
    [Range(0f,1f)] [SerializeField] private float defaultMaster = 1f;
    [Range(0f,1f)] [SerializeField] private float defaultBgm    = 1f;
    [Range(0f,1f)] [SerializeField] private float defaultSfx    = 1f;

    void Awake()
    {
        if (openButton)  openButton.onClick.AddListener(OpenPanel);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefaults);

        SetupSlider(masterSlider, v => { if (GlobalAudio.I) GlobalAudio.I.SetMaster01(v); UpdatePct(masterPercent, v); });
        SetupSlider(bgmSlider,    v => { if (GlobalAudio.I) GlobalAudio.I.SetBgm01(v);    UpdatePct(bgmPercent,    v); });
        SetupSlider(sfxSlider,    v => { if (GlobalAudio.I) GlobalAudio.I.SetSfx01(v);    UpdatePct(sfxPercent,    v); });

        if (panel) panel.SetActive(false);
    }

    void OnEnable() => SyncFromGlobal();

    void SetupSlider(Slider s, System.Action<float> onChanged)
    {
        if (!s) return;
        s.minValue = 0f; s.maxValue = 1f; s.wholeNumbers = false;
        s.onValueChanged.AddListener(v => onChanged(v));
    }

    void SyncFromGlobal()
    {
        if (!GlobalAudio.I) return;
        if (masterSlider) masterSlider.SetValueWithoutNotify(GlobalAudio.I.Master01);
        if (bgmSlider)    bgmSlider.SetValueWithoutNotify(GlobalAudio.I.Bgm01);
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(GlobalAudio.I.Sfx01);

        UpdatePct(masterPercent, GlobalAudio.I.Master01);
        UpdatePct(bgmPercent,    GlobalAudio.I.Bgm01);
        UpdatePct(sfxPercent,    GlobalAudio.I.Sfx01);
    }

    void UpdatePct(TMP_Text t, float v) { if (t) t.text = Mathf.RoundToInt(v*100f) + "%"; }

    public void OpenPanel()  { if (panel) { panel.SetActive(true);  SyncFromGlobal(); } }
    public void ClosePanel() { if (panel) { panel.SetActive(false); } }

    void ResetToDefaults()
    {
        if (GlobalAudio.I)
        {
            GlobalAudio.I.SetMaster01(defaultMaster);
            GlobalAudio.I.SetBgm01(defaultBgm);
            GlobalAudio.I.SetSfx01(defaultSfx);
        }
        if (masterSlider) masterSlider.SetValueWithoutNotify(defaultMaster);
        if (bgmSlider)    bgmSlider.SetValueWithoutNotify(defaultBgm);
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(defaultSfx);

        UpdatePct(masterPercent, defaultMaster);
        UpdatePct(bgmPercent,    defaultBgm);
        UpdatePct(sfxPercent,    defaultSfx);
    }
}
