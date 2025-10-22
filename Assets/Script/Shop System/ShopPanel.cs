using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillLibrary;

public sealed class ShopPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SkillLibrary library;
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private GoldWallet   wallet;

    [Header("UI - Offers")]
    [SerializeField] private OfferWidget[] offers = new OfferWidget[3];
    [SerializeField] private TMP_Text goldText;

    [Header("UI - Owned Actives (4 slots)")]
    [SerializeField] private OwnedSlotWidget[] ownedActiveSlots = new OwnedSlotWidget[4];
    [SerializeField] private Sprite emptyOwnedSprite;
    [SerializeField] private Color  emptyOwnedTint = new Color(1f, 1f, 1f, 0.25f);

    [Serializable]
    public sealed class OfferWidget
    {
        public Button   button;
        public Image    icon;
        public TMP_Text title;
        public TMP_Text price;
        [HideInInspector] public int index;
    }

    [Serializable]
    public sealed class OwnedSlotWidget
    {
        public Image    icon;
        public TMP_Text levelText;
    }

    private struct Offer
    {
        public enum Kind { Active, Passive }
        public Kind kind;
        public ActiveSkillId  aId;
        public PassiveSkillId pId;
        public string title;
        public Sprite icon;
        public int price;
    }

    private readonly List<Offer> _built = new();

    private void Awake()
    {
        for (int i = 0; i < offers.Length; i++)
        {
            var w = offers[i];
            if (w == null) continue;
            w.index = i;
            if (w.button)
            {
                int cap = i;
                w.button.onClick.RemoveAllListeners();
                w.button.onClick.AddListener(() => TryBuy(cap));
            }
        }

        if (!wallet)       wallet       = FindObjectOfType<GoldWallet>(true);
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!library)      library      = FindObjectOfType<SkillLibrary>(true);

        if (wallet)       wallet.OnChanged       += UpdateGoldUI;
        if (playerSkills) playerSkills.OnChanged += RefreshOwnedActives;
    }

    private void OnDestroy()
    {
        if (wallet)       wallet.OnChanged       -= UpdateGoldUI;
        if (playerSkills) playerSkills.OnChanged -= RefreshOwnedActives;
    }

    public void BuildOffers()
    {
        _built.Clear();

        var pool = BuildUnifiedPool();
        for (int i = 0; i < offers.Length; i++)
        {
            if (pool.Count == 0) { _built.Add(default); continue; }
            int pick = UnityEngine.Random.Range(0, pool.Count);
            _built.Add(pool[pick]);
            pool.RemoveAt(pick);
        }

        for (int i = 0; i < offers.Length; i++)
        {
            var w = offers[i];
            if (w == null) continue;

            bool valid = i < _built.Count && _built[i].icon != null;
            if (w.button) w.button.gameObject.SetActive(valid);
            if (!valid) continue;

            var ofr = _built[i];
            if (w.icon)  w.icon.sprite = ofr.icon;
            if (w.title) w.title.text  = ofr.title;
            if (w.price) w.price.text  = ofr.price.ToString();

            bool canAfford = wallet ? wallet.Gold >= ofr.price : true;
            if (w.button) w.button.interactable = canAfford;
        }

        UpdateGoldUI(wallet ? wallet.Gold : 0);
        RefreshOwnedActives();
    }

    private void UpdateGoldUI(int g)
    {
        if (goldText) goldText.text = (wallet != null) ? $"Gold: {g}" : "Gold: --";
        for (int i = 0; i < offers.Length && i < _built.Count; i++)
        {
            var w = offers[i];
            if (w?.button == null) continue;
            bool valid  = _built[i].icon != null;
            bool afford = (wallet == null) || (wallet.Gold >= _built[i].price);
            w.button.interactable = valid && afford;
        }
    }

    private void TryBuy(int idx)
    {
        if (idx < 0 || idx >= _built.Count) return;
        var ofr = _built[idx];
        if (ofr.icon == null) return;

        if (wallet && !wallet.TrySpend(ofr.price)) return;

        bool ok = false;
        if (playerSkills)
        {
            if (ofr.kind == Offer.Kind.Active)
                ok = playerSkills.TryAddOrLevelUp(ofr.aId);
            else
                ok = playerSkills.TryAddOrLevelUp(ofr.pId);
        }

        if (!ok)
        {
            if (wallet) wallet.Add(ofr.price);
            return;
        }

        HideOffer(idx);

        if (AllOffersConsumed())
            BuildOffers();

        RefreshOwnedActives();
    }

    private void HideOffer(int idx)
    {
        if (idx >= 0 && idx < offers.Length)
        {
            var w = offers[idx];
            if (w?.button) w.button.gameObject.SetActive(false);
        }
        if (idx >= 0 && idx < _built.Count)
            _built[idx] = default;
    }

    private bool AllOffersConsumed()
    {
        for (int i = 0; i < offers.Length; i++)
        {
            var w = offers[i];
            if (w?.button && w.button.gameObject.activeSelf)
                return false;
        }
        return true;
    }

    private List<Offer> BuildUnifiedPool()
    {
        var list = new List<Offer>();

        // Actives: add new (if slot available) or level-up (if not max).
        if (library?.actives != null && library.actives.Length > 0)
        {
            for (int i = 0; i < library.actives.Length; i++)
            {
                var e = library.actives[i];
                if (e == null || e.implementation == null || e.icon == null) continue;

                var id    = (ActiveSkillId)i;
                int level = playerSkills.GetLevel(id);
                bool owned = level > 0;

                if (!owned && playerSkills.IsFullActive) continue;
                if (owned && level >= PlayerSkills.MAX_LEVEL) continue;

                list.Add(new Offer
                {
                    kind  = Offer.Kind.Active,
                    aId   = id,
                    title = ActiveTitle(e.displayName, id, level),
                    icon  = e.icon,
                    price = Mathf.Max(0, e.price)
                });
            }
        }

        // Passives: no slot limit; include if not max level.
        if (library?.passives != null && library.passives.Length > 0)
        {
            for (int i = 0; i < library.passives.Length; i++)
            {
                var p = library.passives[i];
                if (p == null || p.icon == null) continue;

                var id    = (PassiveSkillId)i;
                int level = playerSkills.GetLevel(id);

                if (level >= PlayerSkills.MAX_LEVEL) continue;

                list.Add(new Offer
                {
                    kind  = Offer.Kind.Passive,
                    pId   = id,
                    title = PassiveTitle(p.displayName, id, level),
                    icon  = p.icon,
                    price = Mathf.Max(0, p.price)
                });
            }
        }

        return list;
    }

    private void RefreshOwnedActives()
    {
        var eq = playerSkills != null ? playerSkills.Actives : null;

        for (int i = 0; i < ownedActiveSlots.Length; i++)
        {
            var slot = ownedActiveSlots[i];
            if (slot == null) continue;

            if (eq != null && i < eq.Count)
            {
                var id = eq[i];
                var def = library ? library.GetActive(id) : null;
                int lv = Mathf.Max(1, playerSkills.GetLevel(id));

                if (slot.icon)
                {
                    slot.icon.sprite = (def != null && def.icon != null) ? def.icon : emptyOwnedSprite;
                    slot.icon.color  = Color.white;
                }
                if (slot.levelText) slot.levelText.text = $"Lv.{lv}";
            }
            else
            {
                if (slot.icon)
                {
                    slot.icon.sprite = emptyOwnedSprite;
                    slot.icon.color  = emptyOwnedTint;
                }
                if (slot.levelText) slot.levelText.text = "";
            }
        }
    }

    private static string ActiveTitle(string displayName, ActiveSkillId id, int curLv)
    {
        string baseName = string.IsNullOrEmpty(displayName) ? id.ToString() : displayName;
        return curLv > 0 ? $"{baseName} Lv.{curLv + 1}" : baseName;
    }

    private static string PassiveTitle(string displayName, PassiveSkillId id, int curLv)
    {
        string baseName = string.IsNullOrEmpty(displayName) ? id.ToString() : displayName;
        return curLv > 0 ? $"{baseName} Lv.{curLv + 1}" : baseName;
    }
}
