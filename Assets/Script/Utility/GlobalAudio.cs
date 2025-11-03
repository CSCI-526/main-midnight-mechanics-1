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

    const string KEY_MASTER = "vol_master";
    const string KEY_BGM    = "vol_bgm";
    const string KEY_SFX    = "vol_sfx";

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

        _master01 = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        _bgm01    = PlayerPrefs.GetFloat(KEY_BGM,    1f);
        _sfx01    = PlayerPrefs.GetFloat(KEY_SFX,    1f);
        ApplyAll();
    }

    public void SetMaster01(float v){ _master01 = Mathf.Clamp01(v); SetDb(masterParam, _master01); Save(KEY_MASTER, _master01); }
    public void SetBgm01   (float v){ _bgm01    = Mathf.Clamp01(v); SetDb(bgmParam,    _bgm01);    Save(KEY_BGM,    _bgm01);    }
    public void SetSfx01   (float v){ _sfx01    = Mathf.Clamp01(v); SetDb(sfxParam,    _sfx01);    Save(KEY_SFX,    _sfx01);    }

    void ApplyAll()
    {
        SetDb(masterParam, _master01);
        SetDb(bgmParam,    _bgm01);
        SetDb(sfxParam,    _sfx01);
        OnVolumesChanged?.Invoke();
    }

    void SetDb(string param, float v01)
    {
        if (!mixer || string.IsNullOrEmpty(param)) return;
        float db = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
        if (!mixer.SetFloat(param, db))
            Debug.LogError($"[GlobalAudio] Param '{param}' not found on mixer '{mixer}'");
    }

    void Save(string key, float v)
    {
        PlayerPrefs.SetFloat(key, v);
        PlayerPrefs.Save();
        OnVolumesChanged?.Invoke();
    }
}
