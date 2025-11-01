using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChatItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image         badgeImage;    // 可空；无则隐藏
    [SerializeField] private TMP_Text      bodyText;      // 显示 "Name:" 或 "Name: message"
    [SerializeField] private RectTransform emoteStrip;    // 纯表情消息时使用（自动生成多行）

    [Header("Name Formatting")]
    [SerializeField] private bool   colorizeUsernames = true;
    [SerializeField] private bool   boldName          = true;
    [SerializeField] private string colon             = ":";

    [Serializable]
    public struct EmoteEntry
    {
        public string token;     // 例如 "/Pog/"
        public Sprite sprite;    // 单张图片
        [Range(0.25f, 4f)]
        public float scale;      // 单个表情相对缩放
    }

    [Header("Emote Tokens (pure-emote messages only)")]
    [SerializeField] private EmoteEntry[] emotes;

    [Header("Emote Layout")]
    [SerializeField, Range(0.5f, 1f)] private float rowMaxWidthPercent = 0.95f; // 一行可用宽度占比
    [SerializeField, Range(0.8f, 8f)]  private float defaultHeightLines = 2.0f;  // 默认目标高度 = 行数×字号
    [SerializeField, Range(0.8f, 8f)]  private float maxHeightLines     = 3.0f;  // 上限
    [SerializeField, Range(0f, 16f)]   private float emoteSpacing       = 6f;    // 行内表情间距
    [SerializeField, Range(0f, 16f)]   private float rowSpacing         = 4f;    // 行间距
    [SerializeField, Range(0f, 0.5f)]  private float extraSidePaddingEm = 0.06f; // 给每个表情两侧留白（按字号）

    [Header("Badge Sizing")]
    [SerializeField, Range(0.6f, 2.0f)] private float badgeHeightScale = 1.1f;   // 徽章高度 = 字号 * 该倍数（防变形）

    // —— 运行时池 —— //
    readonly List<Image> _emotePool = new();       // 全局图片池（会被分配到各行）
    readonly List<RectTransform> _rowPool = new(); // 行容器池（每行一个 HLG）
    readonly Dictionary<string, EmoteEntry> _emoteMap = new();

    void Awake()
    {
        // token->entry
        _emoteMap.Clear();
        if (emotes != null)
        {
            for (int i = 0; i < emotes.Length; i++)
            {
                if (!string.IsNullOrEmpty(emotes[i].token) && emotes[i].sprite)
                    _emoteMap[emotes[i].token] = emotes[i];
            }
        }

        // 确保 EmoteStrip 是“裸”的 RectTransform（避免你场景里挂了 HLG/LE 导致冲突）
        if (emoteStrip)
        {
            var badHLG = emoteStrip.GetComponent<HorizontalLayoutGroup>();
            if (badHLG) badHLG.enabled = false;
            var badVLG = emoteStrip.GetComponent<VerticalLayoutGroup>();
            if (badVLG) badVLG.enabled = false;
            var badLE  = emoteStrip.GetComponent<LayoutElement>();
            if (badLE)  badLE.enabled  = false;
            var csf    = emoteStrip.GetComponent<ContentSizeFitter>();
            if (csf)    csf.enabled    = false;

            // 若预制体里已有行/图片，收进池
            for (int i = 0; i < emoteStrip.childCount; i++)
            {
                var child = emoteStrip.GetChild(i) as RectTransform;
                if (!child) continue;
                var hlg = child.GetComponent<HorizontalLayoutGroup>();
                if (hlg)
                {
                    child.gameObject.SetActive(false);
                    _rowPool.Add(child);
                    for (int j = 0; j < child.childCount; j++)
                    {
                        var img = child.GetChild(j).GetComponent<Image>();
                        if (img) { img.gameObject.SetActive(false); _emotePool.Add(img); }
                    }
                }
            }
        }
    }

    public void Setup(string username, string message, Sprite badge = null)
    {
        if (!bodyText) return;

        // —— 徽章 —— //
        if (badgeImage)
        {
            if (badge)
            {
                badgeImage.sprite  = badge;
                badgeImage.enabled = true;
                badgeImage.preserveAspect = true; // 防变形
                badgeImage.gameObject.SetActive(true);
                ApplyBadgeSizing();               // ★ 根据字号定高，按比例算宽（防形变）
            }
            else
            {
                badgeImage.sprite  = null;
                badgeImage.enabled = false;
                badgeImage.gameObject.SetActive(false);
            }
        }

        string safeName = Escape(username ?? string.Empty);
        string namePart = colorizeUsernames
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(GetStableColor(safeName))}>{safeName}</color>"
            : safeName;
        if (boldName) namePart = $"<b>{namePart}</b>";
        string useColon = string.IsNullOrEmpty(colon) ? ":" : colon;

        // 解析为 token 列表（按空白分词）
        var tokens = ParseTokens(message);
        bool pureEmote = tokens.Count > 0 && IsAllKnownEmotes(tokens);

        if (pureEmote && emoteStrip)
        {
            // 文本只显示 "Name:" —— 关键：别让 bodyText 抢宽度
            bodyText.richText = true;
            bodyText.text     = $"{namePart}{useColon}";

            // 压缩 bodyText，释放 EmoteStrip 宽度（减少两者间隙）
            SetFlexibleWidth(bodyText.rectTransform, 0f);
            SetFlexibleWidth(emoteStrip, 1f);

            ShowEmoteRows(tokens);
        }
        else
        {
            // 普通文本：恢复 bodyText 的伸展；emoteStrip 隐藏
            SetFlexibleWidth(bodyText.rectTransform, 1f);
            SetFlexibleWidth(emoteStrip, 0f);

            HideEmoteRows();

            string msgEscaped = Escape(message ?? string.Empty);
            bodyText.richText = true;
            bodyText.text     = $"{namePart}{useColon} {msgEscaped}";
        }

        // 刷新布局，避免短暂重叠
        bodyText.ForceMeshUpdate();
        var rt = (RectTransform)transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        var parent = rt.parent as RectTransform;
        if (parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }

    public void ResetItem()
    {
        if (bodyText) bodyText.text = string.Empty;
        HideEmoteRows();
        if (badgeImage)
        {
            badgeImage.sprite  = null;
            badgeImage.enabled = false;
            badgeImage.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    // ============= 表情多行排版 =============

    void ShowEmoteRows(List<string> tokens)
    {
        if (!emoteStrip) return;

        // 确保 emoteStrip 使用 VLG（我们自己管理）
        var vlg = emoteStrip.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = emoteStrip.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.enabled = true;
        vlg.spacing = rowSpacing;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        // 计算一行可用宽度
        float containerW = ((RectTransform)transform).rect.width;
        var parentRT = transform.parent as RectTransform;
        if (containerW <= 0f && parentRT) containerW = parentRT.rect.width;

        float padL = 0f, padR = 0f, rootSpacing = 0f;
        var rootHLG = GetComponent<HorizontalLayoutGroup>();
        if (rootHLG)
        {
            padL = rootHLG.padding.left;
            padR = rootHLG.padding.right;
            rootSpacing = rootHLG.spacing;
        }

        // 徽章、名字占位宽度（名字读 PreferredWidth）
        float badgeW = 0f;
        if (badgeImage && badgeImage.enabled && badgeImage.gameObject.activeInHierarchy)
            badgeW = LayoutUtility.GetPreferredWidth((RectTransform)badgeImage.transform);

        float nameW  = LayoutUtility.GetPreferredWidth((RectTransform)bodyText.transform);

        int rootGaps = (badgeW > 0f ? 2 : 1); // badge→name & name→strip
        float availableW = Mathf.Max(0f, containerW - padL - padR - badgeW - nameW - rootSpacing * rootGaps);
        float maxRowW    = availableW * rowMaxWidthPercent;

        // 目标高度（行高）
        float fontPx   = Mathf.Max(1f, bodyText.fontSize);
        float targetH  = Mathf.Min(maxHeightLines, Mathf.Max(0.8f, defaultHeightLines)) * fontPx;
        float sidePad  = extraSidePaddingEm * fontPx * 2f; // 宽度留白（左右合计）

        // —— 划分多行 —— //
        var rows = new List<List<(Sprite sp, float w, float h)>>();
        var cur  = new List<(Sprite, float, float)>();
        float curW = 0f;

        for (int i = 0; i < tokens.Count; i++)
        {
            var e = _emoteMap[tokens[i]];
            var rect = e.sprite.rect;
            float aspect = rect.height > 0f ? rect.width / rect.height : 1f;

            float h = targetH * Mathf.Max(0.25f, e.scale <= 0f ? 1f : e.scale);
            float w = h * aspect + sidePad;

            float need = (cur.Count == 0 ? w : (curW + emoteSpacing + w));
            if (need > maxRowW && cur.Count > 0)
            {
                rows.Add(cur);
                cur = new List<(Sprite, float, float)>();
                curW = 0f;
                need = w;
            }

            cur.Add((e.sprite, w, h));
            curW = (cur.Count == 1 ? w : (curW + emoteSpacing + w));
        }
        if (cur.Count > 0) rows.Add(cur);

        // —— 创建 / 复用行容器，并填充图片 —— //
        EnsureRows(rows.Count);

        int imageCursor = 0; // 用全局图片池
        for (int r = 0; r < _rowPool.Count; r++)
        {
            var row = _rowPool[r];
            if (r >= rows.Count)
            {
                row.gameObject.SetActive(false);
                continue;
            }

            row.gameObject.SetActive(true);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (!hlg) hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.enabled = true;
            hlg.spacing = emoteSpacing;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0,0,0,0);

            // 行内填充
            var payload = rows[r];
            EnsureImages(imageCursor + payload.Count);

            float rowMaxH = 0f;
            for (int i = 0; i < payload.Count; i++)
            {
                var img = _emotePool[imageCursor + i];
                img.transform.SetParent(row, false);
                img.sprite = payload[i].sp;
                img.preserveAspect = true;
                img.enabled = true;
                img.gameObject.SetActive(true);

                var le = img.GetComponent<LayoutElement>();
                if (!le) le = img.gameObject.AddComponent<LayoutElement>();
                le.minWidth  = le.preferredWidth  = payload[i].w;
                le.minHeight = le.preferredHeight = payload[i].h;

                if (payload[i].h > rowMaxH) rowMaxH = payload[i].h;
            }
            imageCursor += payload.Count;

            // 给行一个首选高度，便于垂直布局累加
            var rowLE = row.GetComponent<LayoutElement>();
            if (!rowLE) rowLE = row.gameObject.AddComponent<LayoutElement>();
            rowLE.minHeight = rowLE.preferredHeight = Mathf.Max(1f, rowMaxH);
        }

        // 关闭未使用的图片
        for (int i = imageCursor; i < _emotePool.Count; i++)
        {
            _emotePool[i].enabled = false;
            _emotePool[i].gameObject.SetActive(false);
        }

        emoteStrip.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(emoteStrip);
    }

    void HideEmoteRows()
    {
        if (!emoteStrip) return;
        for (int i = 0; i < _rowPool.Count; i++)
            _rowPool[i].gameObject.SetActive(false);
        for (int i = 0; i < _emotePool.Count; i++)
        {
            _emotePool[i].enabled = false;
            _emotePool[i].gameObject.SetActive(false);
        }
        emoteStrip.gameObject.SetActive(false);
    }

    void EnsureRows(int need)
    {
        if (!emoteStrip) return;
        while (_rowPool.Count < need)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(emoteStrip, false);
            rt.localScale = Vector3.one;

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.enabled = true;
            hlg.spacing = emoteSpacing;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 10f;

            go.SetActive(false);
            _rowPool.Add(rt);
        }
    }

    void EnsureImages(int need)
    {
        while (_emotePool.Count < need)
        {
            var go = new GameObject("Emote", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.gameObject.SetActive(false);
            _emotePool.Add(img);
        }
    }

    // ============= 工具 =============

    void ApplyBadgeSizing()
    {
        if (!badgeImage || !badgeImage.enabled || !badgeImage.sprite) return;

        var le = badgeImage.GetComponent<LayoutElement>();
        if (!le) le = badgeImage.gameObject.AddComponent<LayoutElement>();

        float fontPx = Mathf.Max(1f, bodyText ? bodyText.fontSize : 16f);
        float h = fontPx * Mathf.Clamp(badgeHeightScale, 0.6f, 2.0f);

        var spr = badgeImage.sprite;
        float aspect = (spr && spr.rect.height > 0f) ? spr.rect.width / spr.rect.height : 1f;

        // 只用高度做基准，宽度按比例计算，避免拉伸变形
        le.minHeight = le.preferredHeight = h;
        le.minWidth  = le.preferredWidth  = h * aspect;
    }

    static void SetFlexibleWidth(RectTransform rt, float flex)
    {
        if (!rt) return;
        var le = rt.GetComponent<LayoutElement>();
        if (!le) le = rt.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;

        // 当我们把某个元素的 flexibleWidth 设为 0 时，用自身首选宽度把它“收紧”，避免占多余空间
        if (flex == 0f)
        {
            float pw = LayoutUtility.GetPreferredWidth(rt);
            le.minWidth = le.preferredWidth = Mathf.Max(1f, pw);
        }
        else
        {
            le.minWidth = -1f;
            le.preferredWidth = -1f;
        }
    }

    static List<string> ParseTokens(string message)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(message)) return list;
        var parts = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++) list.Add(parts[i]);
        return list;
    }

    bool IsAllKnownEmotes(List<string> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
            if (!_emoteMap.ContainsKey(tokens[i]))
                return false;
        return tokens.Count > 0;
    }

    static string Escape(string s) =>
        string.IsNullOrEmpty(s) ? string.Empty :
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    static Color GetStableColor(string username)
    {
        if (string.IsNullOrEmpty(username)) return Color.white;
        unchecked
        {
            int h = 23;
            for (int i = 0; i < username.Length; i++) h = h * 31 + username[i];
            float hue = Mathf.Abs(h % 360) / 360f;
            const float sat = 0.45f, val = 1.00f;
            return Color.HSVToRGB(hue, sat, val, true);
        }
    }
}
