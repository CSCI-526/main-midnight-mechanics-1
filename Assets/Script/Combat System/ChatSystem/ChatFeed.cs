using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatFeed : MonoBehaviour
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect    scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private ChatItem      itemPrefab;

    [Header("Pool & Limits")]
    [SerializeField, Min(0)]  private int prewarm = 30;
    [SerializeField, Min(10)] private int maxVisibleItems = 120;
    [SerializeField, Min(10)] private int poolCap = 200;

    [Header("Username Color")]
    [SerializeField] private bool colorizeUsernames = true;

    [Header("Random Sources")]
    [SerializeField] private string[] usernames;
    [SerializeField] private string[] messages;
    [SerializeField] private Sprite[]  badges;   // 元素可为 null；抽到 null 表示本条无徽章

    [Header("Auto Generate (legacy fallback)")]
    [SerializeField] private bool  autoGenerate = true;
    [SerializeField, Min(0.05f)] private float minInterval = 0.30f;
    [SerializeField, Min(0.05f)] private float maxInterval = 1.20f;

    [Header("Viewer-Linked Rate")]
    [SerializeField] private ViewerSystem viewers;          // 可空，自动查找
    [SerializeField] private bool  rateTiedToViewers = true;
    [SerializeField, Min(1)] private int   refViewers      = 1000;  // 参考观众数
    [SerializeField, Min(0f)] private float baseRateAtRef  = 1.5f;  // 参考观众数时的基础消息速率（条/秒）
    [SerializeField, Range(0.1f, 2.0f)] private float rateExponent = 0.6f; // 速率随观众增长的弯曲程度
    [SerializeField, Min(0f)] private float minRate = 0.05f;       // 下限（条/秒）
    [SerializeField, Min(0f)] private float maxRate = 40f;         // 上限（条/秒）
    [SerializeField] private bool  stopWhenZero = true;            // 观众数为 0 时不再生成

    readonly Queue<ChatItem> _pool = new();
    readonly List<ChatItem>  _live = new();
    readonly Dictionary<string, Color> _nameColors = new();

    bool  _autoScroll = true;
    float _timer;

    void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (!content && scrollRect) content = scrollRect.content;

        if (!scrollRect || !content || !itemPrefab)
        {
            Debug.LogError("[ChatFeed] Bind ScrollRect/Content/ItemPrefab.", this);
            enabled = false; return;
        }

        if (!viewers)
            viewers = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);

        for (int i = 0; i < prewarm; i++)
        {
            var it = Instantiate(itemPrefab, transform);
            it.gameObject.SetActive(false);
            _pool.Enqueue(it);
        }

        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        ScheduleNext();
    }

    void OnDestroy()
    {
        if (scrollRect) scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    void Update()
    {
        if (!autoGenerate) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            PostRandom();
            ScheduleNext();
        }
    }

    // —— 调度下一条 —— //
    void ScheduleNext()
    {
        if (rateTiedToViewers && viewers)
        {
            int v = Mathf.Max(0, viewers.Current);

            if (stopWhenZero && v <= 0)
            {
                // 没人了就不刷；给个较大值防止频繁触发
                _timer = 9999f;
                return;
            }

            // 归一化后做幂次曲线：rate = base * (v/ref)^exp
            float norm   = (refViewers > 0) ? (v / (float)refViewers) : 1f;
            float rate   = baseRateAtRef * Mathf.Pow(Mathf.Max(0.0001f, norm), rateExponent);
            rate         = Mathf.Clamp(rate, minRate, maxRate);  // 条/秒

            _timer = SampleExpInterval(rate); // 指数分布（泊松过程）
            return;
        }

        // 旧逻辑（不绑观众）
        float a = Mathf.Min(minInterval, maxInterval);
        float b = Mathf.Max(minInterval, maxInterval);
        _timer = Random.Range(a, b);
    }

    // 指数分布抽样：返回下一条的“秒数”
    static float SampleExpInterval(float ratePerSec)
    {
        if (ratePerSec <= 0f) return 9999f;
        float u = Random.value;
        if (u < 1e-4f) u = 1e-4f;
        return -Mathf.Log(u) / ratePerSec;
    }

    // —— 手动/随机发 —— //
    public void Post(string username, string message, Sprite badge = null)
    {
        var it = GetItem();
        it.transform.SetParent(content, false);

        var nameCol = colorizeUsernames ? GetStableColor(username ?? string.Empty) : Color.white;
        it.Setup(username ?? string.Empty, message ?? string.Empty, badge);

        _live.Add(it);
        CullOldest();

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (_autoScroll) ScrollToBottom();
    }

    public void PostRandom()
    {
        if (usernames == null || usernames.Length == 0) return;
        if (messages  == null || messages.Length  == 0) return;

        string u = usernames[Random.Range(0, usernames.Length)];
        string m = messages[ Random.Range(0, messages.Length )];

        Sprite badge = null;
        if (badges != null && badges.Length > 0)
            badge = badges[Random.Range(0, badges.Length)]; // 可能为 null → 不显示徽章

        Post(u, m, badge);
    }

    public void ClearAll()
    {
        for (int i = 0; i < _live.Count; i++) ReturnItem(_live[i]);
        _live.Clear();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        ScrollToBottom(true);
    }

    void CullOldest()
    {
        while (_live.Count > maxVisibleItems)
        {
            var oldest = _live[0];
            _live.RemoveAt(0);
            ReturnItem(oldest);
        }
    }

    ChatItem GetItem()
    {
        if (_pool.Count > 0)
        {
            var it = _pool.Dequeue();
            it.gameObject.SetActive(true);
            return it;
        }
        return Instantiate(itemPrefab);
    }

    void ReturnItem(ChatItem it)
    {
        if (!it) return;
        it.transform.SetParent(transform, false);
        it.ResetItem();
        if (_pool.Count >= poolCap) Destroy(it.gameObject);
        else _pool.Enqueue(it);
    }

    void ScrollToBottom(bool instant = false)
    {
        if (instant)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void OnScrollValueChanged(Vector2 v)
    {
        const float threshold = 0.05f;
        _autoScroll = v.y <= threshold || !scrollRect.verticalScrollbar || !scrollRect.vertical;
    }

    // —— 稳定用户名配色（保留供外部使用） —— //
    Color GetStableColor(string username)
    {
        if (_nameColors.TryGetValue(username, out var c)) return c;

        unchecked
        {
            int h = 23;
            for (int i = 0; i < username.Length; i++) h = h * 31 + username[i];
            float hue = Mathf.Abs(h % 360) / 360f;
            const float sat = 0.45f, val = 1.00f;
            var col = Color.HSVToRGB(hue, sat, val, true);
            _nameColors[username] = col;
            return col;
        }
    }
}
