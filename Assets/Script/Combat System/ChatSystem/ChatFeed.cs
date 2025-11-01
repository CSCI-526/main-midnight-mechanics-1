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
    [SerializeField] private Sprite[]  badges;

    [Header("Auto Generate")]
    [SerializeField] private bool  autoGenerate = true;

    [Header("Rate (Unified)")]
    [SerializeField] private ViewerSystem viewers;    // 可空→自动找
    [SerializeField] private float baseEpmAtBaseline = 24f; // 基线（观众=baseline）每分钟发言
    [SerializeField] private float minIntervalClamp  = 0.08f;
    [SerializeField] private float maxIntervalClamp  = 6.00f;

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

        if (!viewers) viewers = FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);

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
        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f)
        {
            PostRandom();
            ScheduleNext();
        }
    }

    void ScheduleNext()
    {
        float mult  = viewers ? viewers.GetUnifiedRateMultiplier() : 1f;
        float epm   = Mathf.Max(0.01f, baseEpmAtBaseline * mult);
        float lam   = epm / 60f; // events/sec
        float u     = Mathf.Clamp01(Random.value);
        float dt    = -Mathf.Log(1f - u) / Mathf.Max(0.0001f, lam); // 指数分布
        _timer      = Mathf.Clamp(dt, minIntervalClamp, maxIntervalClamp);
    }

    public void Post(string username, string message, Sprite badge = null)
    {
        var it = GetItem();
        it.transform.SetParent(content, false);

        var nameCol = colorizeUsernames ? GetStableColor(username ?? string.Empty) : Color.white;
        it.Setup(username ?? string.Empty, message ?? string.Empty, nameCol, badge);

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
        string m = messages [Random.Range(0, messages.Length )];

        Sprite badge = null;
        if (badges != null && badges.Length > 0)
            badge = badges[Random.Range(0, badges.Length)]; // 可能为 null

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

    Color GetStableColor(string username)
    {
        if (_nameColors.TryGetValue(username, out var c)) return c;
        unchecked
        {
            int h = 23;
            for (int i = 0; i < username.Length; i++) h = h * 31 + username[i];
            float hue = Mathf.Abs(h % 360) / 360f;
            var col = Color.HSVToRGB(hue, 0.45f, 1.0f, true);
            _nameColors[username] = col;
            return col;
        }
    }
}
