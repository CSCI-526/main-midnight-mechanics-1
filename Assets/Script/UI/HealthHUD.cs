using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HealthHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth player;
    [SerializeField] private RectTransform row;   // 容器
    [SerializeField] private Image heartPrefab;   // 预制（禁用状态，作为模板）

    [Header("Sprites")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    // 内部缓存
    private readonly List<Image> _hearts = new();
    private int _lastMax = -1;

    void Awake()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>(true);
        if (!row) row = transform as RectTransform;
        if (!heartPrefab)
            Debug.LogError("[HealthHUD] heartPrefab missing.");
    }

    void OnEnable()
    {
        if (player != null) player.OnHealthChanged += Sync;
        // 首次同步
        if (player != null) Sync(player.CurrentHp, player.MaxHp);
    }

    void OnDisable()
    {
        if (player != null) player.OnHealthChanged -= Sync;
    }

    void Sync(int current, int max)
    {
        if (max != _lastMax) Rebuild(max);
        UpdateHearts(current);
    }

    void Rebuild(int max)
    {
        // 
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            var child = row.GetChild(i);
            if (heartPrefab && child.gameObject == heartPrefab.gameObject) continue;
            Destroy(child.gameObject);
        }
        _hearts.Clear();

        if (!heartPrefab) return;

        // 
        for (int i = 0; i < max; i++)
        {
            var img = Instantiate(heartPrefab, row);
            img.gameObject.SetActive(true);
            _hearts.Add(img);
        }

        // 
        if (heartPrefab.gameObject.activeSelf) heartPrefab.gameObject.SetActive(false);

        _lastMax = max;
    }

    void UpdateHearts(int current)
    {
        for (int i = 0; i < _hearts.Count; i++)
        {
            var img = _hearts[i];
            if (!img) continue;

            bool filled = i < current;
            img.sprite = filled ? fullHeart : emptyHeart;
            img.color  = Color.white;
        }
    }
}
