using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-1000)]
public class GlobalAudio : MonoBehaviour
{
    public static GlobalAudio I { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SfxGroup   => sfxGroup;

    [Header("Exposed Param Names")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam    = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    [Header("Defaults (used every run)")]
    [Range(0f,1f)] [SerializeField] private float defaultMaster = 1f;
    [Range(0f,1f)] [SerializeField] private float defaultBgm    = 0f; // Web 初始静音 BGM 可设 0
    [Range(0f,1f)] [SerializeField] private float defaultSfx    = 1f;

    [Header("dB Mapping")]
    [SerializeField] private float muteFloorDb = -80f; // 0 → -80 dB
    [SerializeField] private float maxDb       = 0f;    // 1 →   0 dB

    float _master01 = 1f, _bgm01 = 1f, _sfx01 = 1f;
    public float Master01 => _master01;
    public float Bgm01    => _bgm01;
    public float Sfx01    => _sfx01;

    public System.Action OnVolumesChanged;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // 每次运行都用 Inspector 默认值（不持久化）
        _master01 = Mathf.Clamp01(defaultMaster);
        _bgm01    = Mathf.Clamp01(defaultBgm);
        _sfx01    = Mathf.Clamp01(defaultSfx);

        ApplyAll(); // 先应用一遍
    }

    void Start()
    {
        // 首帧补丁：等所有 AudioSource 绑定好 Output 再重应用两次，避免“初始不生效”
        StartCoroutine(ReapplyAfterBootstrap());
    }

    IEnumerator ReapplyAfterBootstrap()
    {
        yield return new WaitForEndOfFrame();
        ApplyAll();

        double target = AudioSettings.dspTime + 0.05;
        while (AudioSettings.dspTime < target) yield return null;
        ApplyAll();
    }

    // —— Public API —— //
    public void SetMaster01(float v)
    {
        _master01 = Mathf.Clamp01(v);
        SetDb(masterParam, _master01);
        AudioListener.volume = _master01; // 兜底
        OnVolumesChanged?.Invoke();
    }

    public void SetBgm01(float v)
    {
        _bgm01 = Mathf.Clamp01(v);
        SetDb(bgmParam, _bgm01);
        OnVolumesChanged?.Invoke();
    }

    public void SetSfx01(float v)
    {
        _sfx01 = Mathf.Clamp01(v);
        SetDb(sfxParam, _sfx01);
        OnVolumesChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        _master01 = Mathf.Clamp01(defaultMaster);
        _bgm01    = Mathf.Clamp01(defaultBgm);
        _sfx01    = Mathf.Clamp01(defaultSfx);
        ApplyAll();
        OnVolumesChanged?.Invoke();
    }

    /// 当你刚把 AudioSource 的 Output 指到 Music/SFX 组之后，立刻调用确保当前音量生效
    public void ReapplyMixerNow() => ApplyAll();

    // —— Internals —— //
    void ApplyAll()
    {
        SetDb(masterParam, _master01);
        SetDb(bgmParam,    _bgm01);
        SetDb(sfxParam,    _sfx01);
        AudioListener.volume = Mathf.Clamp01(_master01); // 再兜底一次
    }

    void SetDb(string param, float v01)
    {
        if (!mixer || string.IsNullOrEmpty(param)) return;
        float db = (v01 <= 0.0001f) ? muteFloorDb : Mathf.Clamp(20f * Mathf.Log10(v01), muteFloorDb, maxDb);
        mixer.SetFloat(param, db);
    }
}
