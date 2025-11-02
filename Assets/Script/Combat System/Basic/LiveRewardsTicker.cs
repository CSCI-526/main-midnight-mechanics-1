using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class LiveRewardTicker : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text tickerText;
    [SerializeField] private float fadeInSeconds  = 0.25f;
    [SerializeField] private float showSeconds    = 2.00f;
    [SerializeField] private float fadeOutSeconds = 0.35f;

    [Header("Data Sources")]
    [SerializeField] private ViewerSystem viewers;
    [SerializeField] private string[] usernames;

    [Header("Event Mix")]
    [Range(0f,1f)] [SerializeField] private float donateProbability = 0.30f;
    [Range(0f,1f)] [SerializeField] private float giftedProbability = 0.15f;

    [Header("Donation Sampling")]
    [SerializeField] private int   donateMin       = 3;
    [SerializeField] private int   donateMax       = 100000;
    [SerializeField] private float donateAlpha     = 2.2f;
    [SerializeField] private bool  quantizeDonation = true;

    [Header("Superchat (donation with message)")]
    [SerializeField] private bool   superchatEnabled = true;
    [SerializeField, Range(0f,1f)] private float superchatProbabilityInDonate = 0.30f;
    [SerializeField] private string[] superchatMessages;

    [Header("Gifted Subs")]
    [SerializeField] private int[] giftSubOptions = new int[] { 5, 10 };

    [Header("Unified Rate")]
    [SerializeField] private float baseEpmAtBaseline = 4f;
    [SerializeField] private float minIntervalClamp  = 0.40f;
    [SerializeField] private float maxIntervalClamp  = 12.0f;

    // === Only TWO colors ===
    [Header("Colors (Two-Color Scheme)")]
    [SerializeField] private Color primaryColor   = new Color(0.90f, 0.95f, 1.00f); // username, $amount, Subscribed!, xN Subscriptions
    [SerializeField] private Color secondaryColor = new Color(0.85f, 0.85f, 0.95f); // verbs & superchat body: "just", "Donate", "just gifted", message

    [Header("Font Sizes")]
    [SerializeField] private float otherFontSize = 28f;             // base font size
    [SerializeField] private float superchatMessageFontSize = 22f;  // superchat body smaller
    [SerializeField] private float headerSizeMul  = 1.15f;          // superchat header size multiplier

    [Header("Mirror to Chat (optional)")]
    [SerializeField] private ChatFeed chatFeed;
    [SerializeField] private bool     mirrorToChat = true;
    [SerializeField] private Sprite   subBadge;
    [SerializeField] private Sprite   donateBadge;
    [SerializeField] private Sprite   giftBadge;

    [Header("Earnings (session total)")]
    [SerializeField] private TMP_Text earningsText;
    [SerializeField] private int      subPayoutUSD = 5;
    [SerializeField] private float    usdPerThousandViewerMinutes = 0.25f;
    [SerializeField] private float    passiveUpdateEverySeconds = 0.5f;
    [SerializeField] private bool     passiveUseUnifiedMultiplier = true;
    [SerializeField] private bool     displayAbbreviated = true;

    [Header("Earnings UI FX")]
    [SerializeField] private float earningsFadeOutSeconds = 0.15f;
    [SerializeField] private float earningsFadeInSeconds  = 0.15f;

    public int TotalEarningsUSD { get; private set; }
    public event Action<int> OnEarningsChanged;

    float _spawnTimer;
    readonly Queue<string> _queue = new();
    bool _active;
    float _tFadeIn, _tShow, _tFadeOut;

    float _passiveTimer;
    double _earnFracAcc;
    Coroutine _earningsFxCo;

    void Awake()
    {
        if (!tickerText) tickerText = GetComponentInChildren<TMP_Text>(true);
        if (!viewers)    viewers    = UnityEngine.Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        if (!tickerText) { Debug.LogError("[LiveRewardTicker] Assign tickerText.", this); enabled = false; return; }

        tickerText.enableAutoSizing = false;
        tickerText.fontSize = otherFontSize;
        tickerText.text  = string.Empty;
        tickerText.alpha = 0f;

        UpdateEarningsLabel(false);
        ScheduleNext();
    }

    void Update()
    {
        PassiveEarningTick();

        _spawnTimer -= Time.unscaledDeltaTime;
        if (_spawnTimer <= 0f)
        {
            float r = UnityEngine.Random.value;
            if (r < donateProbability)
            {
                int amount = SampleDonation();
                bool asSuper = superchatEnabled && superchatMessages != null && superchatMessages.Length > 0
                               && UnityEngine.Random.value < superchatProbabilityInDonate;
                string u = RandomUsername();

                if (asSuper)
                {
                    string msg = superchatMessages[UnityEngine.Random.Range(0, superchatMessages.Length)];
                    _queue.Enqueue(FormatSuperchat(u, amount, msg));
                    AddEarning(amount);
                    MirrorDonateToChat(u, amount, msg);
                }
                else
                {
                    _queue.Enqueue(FormatDonate(u, amount));
                    AddEarning(amount);
                    MirrorDonateToChat(u, amount, null);
                }
            }
            else if (r < donateProbability + giftedProbability)
            {
                string u = RandomUsername();
                int giftCount = PickGiftCount();
                _queue.Enqueue(FormatGifted(u, giftCount));
                AddEarning(subPayoutUSD * Mathf.Max(1, giftCount));
                MirrorGiftToChat(u, giftCount);
            }
            else
            {
                string u = RandomUsername();
                _queue.Enqueue(FormatSub(u));
                AddEarning(subPayoutUSD);
                MirrorSubToChat(u);
            }

            ScheduleNext();
        }

        if (!_active && _queue.Count > 0) StartNext(_queue.Dequeue());
        if (_active)
        {
            if (_tFadeIn > 0f)
            {
                _tFadeIn -= Time.unscaledDeltaTime;
                tickerText.alpha = 1f - Mathf.Clamp01(_tFadeIn / Mathf.Max(0.0001f, fadeInSeconds));
                if (_tFadeIn <= 0f) _tShow = showSeconds;
            }
            else if (_tShow > 0f)
            {
                _tShow -= Time.unscaledDeltaTime;
                tickerText.alpha = 1f;
                if (_tShow <= 0f) _tFadeOut = fadeOutSeconds;
            }
            else if (_tFadeOut > 0f)
            {
                _tFadeOut -= Time.unscaledDeltaTime;
                tickerText.alpha = Mathf.Clamp01(_tFadeOut / Mathf.Max(0.0001f, fadeOutSeconds));
                if (_tFadeOut <= 0f)
                {
                    tickerText.text = string.Empty;
                    tickerText.alpha = 0f;
                    _active = false;
                }
            }
        }
    }

    void PassiveEarningTick()
    {
        _passiveTimer += Time.unscaledDeltaTime;
        if (_passiveTimer < passiveUpdateEverySeconds) return;

        float dt = _passiveTimer;
        _passiveTimer = 0f;

        int viewersNow = viewers ? viewers.Current : 0;
        if (viewersNow <= 0) return;

        double viewerMinutes = viewersNow * (dt / 60.0);
        double usd = viewerMinutes * (usdPerThousandViewerMinutes / 1000.0);

        if (passiveUseUnifiedMultiplier && viewers)
        {
            float mul = Mathf.Max(1f, viewers.GetUnifiedRateMultiplier());
            usd *= mul;
        }

        _earnFracAcc += usd;
        int whole = (int)Math.Floor(_earnFracAcc);
        if (whole > 0)
        {
            _earnFracAcc -= whole;
            AddEarning(whole);
        }
    }

    void ScheduleNext()
    {
        float mult = viewers ? viewers.GetUnifiedRateMultiplier() : 1f;
        float epm  = Mathf.Max(0.01f, baseEpmAtBaseline * mult);
        float lam  = epm / 60f;
        float u    = Mathf.Clamp01(UnityEngine.Random.value);
        float dt   = -Mathf.Log(1f - u) / Mathf.Max(0.0001f, lam);
        _spawnTimer = Mathf.Clamp(dt, minIntervalClamp, maxIntervalClamp);
    }

    string RandomUsername()
    {
        if (usernames != null && usernames.Length > 0)
            return usernames[UnityEngine.Random.Range(0, usernames.Length)];
        return "viewer";
    }

    int PickGiftCount()
    {
        if (giftSubOptions == null || giftSubOptions.Length == 0) return 5;
        return giftSubOptions[UnityEngine.Random.Range(0, giftSubOptions.Length)];
    }

    int SampleDonation()
    {
        int lo = Mathf.Max(1, donateMin);
        int hi = Mathf.Max(lo, donateMax);
        float alpha = Mathf.Max(1.01f, donateAlpha);

        float u = Mathf.Clamp01(UnityEngine.Random.value);
        float a1 = 1f - alpha;
        double loPow = Math.Pow(lo, a1);
        double hiPow = Math.Pow(hi, a1);
        double xPow  = loPow + (hiPow - loPow) * u;
        double xCont = Math.Pow(xPow, 1.0 / a1);
        int raw = Mathf.Clamp(Mathf.RoundToInt((float)xCont), lo, hi);

        if (!quantizeDonation) return raw;

        int step =
            raw < 20     ? 1  :
            raw < 100    ? 5  :
            raw < 500    ? 10 :
            raw < 2000   ? 25 :
            raw < 10000  ? 50 :
            raw < 50000  ? 100 :
            500;

        int q = Mathf.RoundToInt(Mathf.Round(raw / (float)step) * step);
        return Mathf.Clamp(q, lo, hi);
    }

    // === Two-color formatting ===
    string FormatSub(string username)
    {
        // primary: username + "Subscribed!"
        // secondary: " just "
        string u     = Colorize(username, primaryColor);
        string verb  = Colorize(" just ", secondaryColor);
        string tail  = Colorize("Subscribed!", primaryColor);
        return u + verb + tail;
    }

    string FormatGifted(string username, int count)
    {
        // secondary: " just gifted "
        // primary: "x{count} Subscriptions"
        string u     = Colorize(username, primaryColor);
        string verb  = Colorize(" just gifted ", secondaryColor);
        string tail  = Colorize($"x{count} Subscriptions", primaryColor);
        return u + verb + tail;
    }

    string FormatDonate(string username, int amount)
    {
        // secondary: " Donate "
        // primary: username + $"${amount}"
        string u     = Colorize(username, primaryColor);
        string verb  = Colorize(" Donate ", secondaryColor);
        string money = Colorize($"${amount}", primaryColor);
        return u + verb + money;
    }

    string FormatSuperchat(string username, int amount, string message)
    {
        // header: username(primary) + " Donate "(secondary) + amount(primary)
        // body: message(secondary), smaller font
        float headerAbs = Mathf.Max(1f, otherFontSize * Mathf.Max(0.1f, headerSizeMul));
        float bodyAbs   = Mathf.Max(1f, superchatMessageFontSize);

        string u     = Colorize(username, primaryColor);
        string verb  = Colorize(" Donate ", secondaryColor);
        string money = Colorize($"${amount}", primaryColor);

        string header = $"<size={headerAbs}>{u}{verb}{money}</size>";
        string body   = Colorize($"<size={bodyAbs}>{Escape(message)}</size>", secondaryColor);
        return header + "\n" + body;
    }

    // === Render / Mirror / Earnings ===
    void StartNext(string richText)
    {
        tickerText.fontSize = otherFontSize;
        tickerText.text  = richText ?? string.Empty;
        tickerText.alpha = 0f;
        _tFadeIn  = fadeInSeconds;
        _tShow    = 0f;
        _tFadeOut = 0f;
        _active   = true;
    }

    void MirrorSubToChat(string username)
    {
        if (!mirrorToChat || !chatFeed) return;
        chatFeed.Post(username, "just subscribed!", subBadge);
    }

    void MirrorGiftToChat(string username, int count)
    {
        if (!mirrorToChat || !chatFeed) return;
        chatFeed.Post(username, $"just gifted x{count} subs!", giftBadge ? giftBadge : subBadge);
    }

    void MirrorDonateToChat(string username, int amount, string superchatMsgOrNull)
    {
        if (!mirrorToChat || !chatFeed) return;
        if (string.IsNullOrEmpty(superchatMsgOrNull))
            chatFeed.Post(username, $"donated ${amount}!", donateBadge);
        else
            chatFeed.Post(username, $"donated ${amount} - {superchatMsgOrNull}", donateBadge);
    }

    void AddEarning(int usd)
    {
        if (usd <= 0) return;
        TotalEarningsUSD += usd;
        OnEarningsChanged?.Invoke(TotalEarningsUSD);
        UpdateEarningsLabel(true);
    }

    void UpdateEarningsLabel(bool animate)
    {
        if (!earningsText) return;
        string txt = displayAbbreviated ? AbbrevUSD(TotalEarningsUSD) : $"${TotalEarningsUSD:N0}";

        if (!animate || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            earningsText.alpha = 1f;
            earningsText.SetText(txt);
            return;
        }

        if (_earningsFxCo != null) StopCoroutine(_earningsFxCo);
        _earningsFxCo = StartCoroutine(CoEarningsFade(txt));
    }

    System.Collections.IEnumerator CoEarningsFade(string newText)
    {
        if (!earningsText) yield break;

        float t = 0f;
        float outDur = Mathf.Max(0f, earningsFadeOutSeconds);
        float inDur  = Mathf.Max(0f, earningsFadeInSeconds);

        if (outDur > 0f)
        {
            float a0 = earningsText.alpha;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outDur);
                earningsText.alpha = Mathf.Lerp(a0, 0f, k);
                yield return null;
            }
        }
        else
        {
            earningsText.alpha = 0f;
        }

        earningsText.SetText(newText);

        t = 0f;
        if (inDur > 0f)
        {
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / inDur);
                earningsText.alpha = k;
                yield return null;
            }
        }

        earningsText.alpha = 1f;
        _earningsFxCo = null;
    }

    void UpdateEarningsLabel() => UpdateEarningsLabel(false);

    string AbbrevUSD(int val)
    {
        if (val >= 1_000_000_000) return $"${val / 1_000_000_000f:0.##}B";
        if (val >= 1_000_000)     return $"${val / 1_000_000f:0.##}M";
        if (val >= 1_000)         return $"${val / 1_000f:0.##}K";
        return $"${val:N0}";
    }

    // === utils ===
    string Colorize(string s, Color c)
    {
        Color32 c32 = c;
        string hex = ColorUtility.ToHtmlStringRGBA(c32);
        return $"<color=#{hex}>{s}</color>";
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");
    }
}
