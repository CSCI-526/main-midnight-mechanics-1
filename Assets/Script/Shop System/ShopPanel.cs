using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Skills; 

public sealed class ShopPanel : MonoBehaviour
{
    [Header("Panel Root & Button")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button     nextButton;

    [Header("数据")]
    [SerializeField] private SkillLibrary library; 
    [SerializeField] private PlayerSkills playerSkills;

    [Header("UI Roots")]
    [SerializeField] private RectTransform sellerPanel;
    [SerializeField] private ShopDropZone   sellerZone;
    [SerializeField] private RectTransform  ownedPanel;
    [SerializeField] private ShopDropZone[] ownedSlots = new ShopDropZone[4];

    [Header("DragLayer（可选统一注入）")]
    [SerializeField] private Transform sharedDragLayer;

    // runtime
    private readonly Dictionary<SkillLibrary.ActiveSkillId, DraggableSkillCard> _cardById =
        new Dictionary<SkillLibrary.ActiveSkillId, DraggableSkillCard>();
    private readonly List<DraggableSkillCard> _allCards =
        new List<DraggableSkillCard>();
    private readonly Dictionary<ActiveSkillBase, SkillLibrary.ActiveSkillId> _implToId =
        new Dictionary<ActiveSkillBase, SkillLibrary.ActiveSkillId>();

    private Action _onNext;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (!panelRoot) Debug.LogError("[ShopPanel] panelRoot 未赋值。");
        if (panelRoot && panelRoot.activeSelf) panelRoot.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNext);
            nextButton.interactable = false;     // ★ 初始先灰
        }

        BuildImplToIdMap();
        BuildCardsFromScene();
        SyncFromPlayerSkills();
        UpdateContinueInteractable();            // ★ 按已装备数量决定是否点亮
    }

    void OnEnable()  { if (playerSkills) playerSkills.OnChanged += HandleEquipChanged; }
    void OnDisable() { if (playerSkills) playerSkills.OnChanged -= HandleEquipChanged; }

    // 显/隐
    public void Show(Action onNext)
    {
        _onNext = onNext;
        IsOpen = true;

        if (!panelRoot) { Debug.LogError("[ShopPanel] Show失败：panelRoot为空。"); return; }

        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);

        Time.timeScale = 0f;
        Refresh();
    }

    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (panelRoot) panelRoot.SetActive(false);
        Time.timeScale = 1f;

        var cb = _onNext; _onNext = null;
        cb?.Invoke();
    }

    // // ★ 兜底：未满足条件不可继续
    // private void HandleNext()
    // {
    //     if (!CanContinue()) return;
    //     Hide();
    // }

    private void HandleNext()
    {
        if (!CanContinue()) return;

        // Analytics Log currently equipped skills
        if (playerSkills != null)
        {
            List<string> equippedNames = new List<string>();
            foreach (var id in playerSkills.Actives)
            {
                equippedNames.Add(id.ToString());
            }

            SkillsTracker.Instance?.LogEquippedSkills(equippedNames);
        }

        Hide();
    }




    public void SetNextInteractable(bool on)
    {
        if (nextButton) nextButton.interactable = on;
    }

    public void Refresh()
    {
        BuildImplToIdMap();
        BuildCardsFromScene();
        SyncFromPlayerSkills();
        UpdateContinueInteractable();
        Canvas.ForceUpdateCanvases();
    }

    void HandleEquipChanged() => UpdateContinueInteractable();

    // ★ 至少装备 1 个才可继续
    bool CanContinue()
    {
        return playerSkills != null
            && playerSkills.Actives != null
            && playerSkills.Actives.Count >= 1;
    }

    void UpdateContinueInteractable()
    {
        SetNextInteractable(CanContinue());
    }

    // ---------- 映射 ----------
    void BuildImplToIdMap()
    {
        _implToId.Clear();
        if (!library) return;

        // ★ 新版库只有 GetImpl；固定 8 个枚举位
        for (int i = 0; i < 8; i++)
        {
            var id   = (SkillLibrary.ActiveSkillId)i;
            var impl = library.GetImpl(id);
            if (impl) _implToId[impl] = id;
        }
    }

    // ---------- 扫描 sellerPanel 下的卡片 ----------
    void BuildCardsFromScene()
    {
        _cardById.Clear();
        _allCards.Clear();

        if (!sellerPanel) return;

        var cards = sellerPanel.GetComponentsInChildren<DraggableSkillCard>(includeInactive: true);
        foreach (var card in cards)
        {
            if (!card) continue;

            // 统一注入 DragLayer（避免每张卡配置）
            if (sharedDragLayer && !card.HasDragLayer())
                card.SetDragLayer(sharedDragLayer);

            _allCards.Add(card);

            if (TryGetIdForCard(card, out var id) && !_cardById.ContainsKey(id))
                _cardById.Add(id, card);
        }
    }

    bool TryGetIdForCard(DraggableSkillCard card, out SkillLibrary.ActiveSkillId id)
    {
        id = default;
        if (!card) return false;
        var sk = card.Skill;
        if (!sk) return false;
        return _implToId.TryGetValue(sk, out id);
    }

    // ---------- 同步：把已装备的摆进槽 ----------
    void SyncFromPlayerSkills()
    {
        foreach (var c in _allCards)
            if (c) c.SnapToHome();

        if (!playerSkills || ownedSlots == null || ownedSlots.Length == 0) return;

        int placed = 0;
        var eq = playerSkills.Actives;
        for (int i = 0; i < eq.Count && placed < ownedSlots.Length; i++)
        {
            var id = eq[i];
            if (!_cardById.TryGetValue(id, out var card) || !card) continue;

            int slotIdx = FindFirstEmptySlot();
            if (slotIdx < 0) break;

            var slotZone = ownedSlots[slotIdx];
            var slotParent = GetSlotContentParent(slotZone); // 使用内容父物体
            if (slotParent)
            {
                card.MarkDroppedTo(slotParent, intoSlot: true);
                placed++;
            }
        }
    }

    int FindFirstEmptySlot()
    {
        if (ownedSlots == null) return -1;
        for (int i = 0; i < ownedSlots.Length; i++)
        {
            var slotParent = GetSlotContentParent(ownedSlots[i]);
            if (slotParent && !SlotHasAnyCard(slotParent))
                return i;
        }
        return -1;
    }

    // ---------- Drop 入口 ----------
    public void HandleDrop(ShopDropZone zone, DraggableSkillCard card)
    {
        if (!zone || !card) return;

        if (zone.ZoneKind == ShopDropZone.Kind.Slot)
        {
            var slotParent = GetSlotContentParent(zone);
            if (!slotParent) { Debug.LogWarning("[ShopPanel] 槽位缺少有效的内容父物体。"); return; }

            if (SlotHasAnyCard(slotParent))
            {
                Debug.Log("[ShopPanel] 槽位已有卡片，忽略本次放置。");
                return;
            }

            if (!TryGetIdForCard(card, out var id))
            {
                Debug.LogWarning("[ShopPanel] Drop失败：卡片未映射到 ActiveSkillId（请检查 SkillLibrary 实现数组）。");
                return;
            }
            if (!playerSkills || !playerSkills.TryAdd(id))
            {
                Debug.LogWarning("[ShopPanel] Drop失败：PlayerSkills.TryAdd(id) 返回 false（可能已满/已存在）。");
                return;
            }

            card.MarkDroppedTo(slotParent, intoSlot: true);
            Debug.Log($"[ShopPanel] Equipped {id} into {zone.name}.");
            UpdateContinueInteractable();
            return;
        }

        // sellerZone：卸下并回“家”
        if (TryGetIdForCard(card, out var rid))
            playerSkills?.Remove(rid);

        card.SnapToHome();
        UpdateContinueInteractable();
    }

    // 槽外松手（从槽位起拖放空白）：卸下并回“家”
    public void ForceReturnToSeller(DraggableSkillCard card)
    {
        if (!card) return;

        if (TryGetIdForCard(card, out var id))
            playerSkills?.Remove(id);

        card.SnapToHome();
        UpdateContinueInteractable();
    }

    // ===== 辅助：只把“有卡”的子节点当占用 =====
    private static bool SlotHasAnyCard(Transform slotParent)
    {
        if (!slotParent) return false;
        for (int i = 0; i < slotParent.childCount; i++)
            if (slotParent.GetChild(i).GetComponent<DraggableSkillCard>())
                return true;
        return false;
    }

    // ===== 辅助：优先使用名为 "Content" 的子节点作为放置目标，否则用 zone 自己 =====
    private static Transform GetSlotContentParent(ShopDropZone zone)
    {
        if (!zone) return null;
        var t = zone.transform;
        var content = t.Find("Content");
        return content ? content : t;
    }
}
